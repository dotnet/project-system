// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Provides search results for non-top-level dependency tree nodes of all projects across the solution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Because we only materialize dependencies tree items when needed, we cannot assume here that search results
    /// coincide with existing tree items. Instead, for each match we find we create a new tree item. The tree then
    /// calls back via the 'contained by' relationship to determine the ancestors of result items, repeating the
    /// process until a known node is returned. For our purposes, these 'root' nodes are <see cref="IVsHierarchyItem"/>
    /// instances that correspond to top-level project dependencies.
    /// </para>
    /// <para>
    /// Top level dependencies participate in search for free, via the hierarchy.
    /// </para>
    /// </remarks>
    [Export(typeof(ISearchProvider))]
    internal sealed class DependenciesSearchProvider : ISearchProvider
    {
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly IVsHierarchyItemManager _hierarchyItemManager;
        private readonly IProjectServiceAccessor _projectServiceAccessor;

        [ImportingConstructor]
        public DependenciesSearchProvider(
            JoinableTaskContext joinableTaskContext,
            IVsHierarchyItemManager hierarchyItemManager,
            IProjectServiceAccessor projectServiceAccessor)
        {
            _joinableTaskContext = joinableTaskContext;
            _hierarchyItemManager = hierarchyItemManager;
            _projectServiceAccessor = projectServiceAccessor;
        }

        public void Search(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
        {
            Requires.NotNull(parameters, nameof(parameters));
            Requires.NotNull(resultAccumulator, nameof(resultAccumulator));

            if (!parameters.Options.SearchExternalItems)
            {
                // Consider the dependencies tree as containing 'external items', allowing the
                // tree to be excluded from search results via this option.
                return;
            }

            using var context = new DependenciesSearchContext(parameters, resultAccumulator);

            _joinableTaskContext.Factory.Run(SearchAsync);

            return;

            async Task SearchAsync()
            {
                // Search projects concurrently
                await Task.WhenAll(_projectServiceAccessor.GetProjectService().LoadedUnconfiguredProjects.Select(SearchProjectAsync));
            }

            async Task SearchProjectAsync(UnconfiguredProject unconfiguredProject)
            {
                IAssetsFileDependenciesDataSource? dataSource = unconfiguredProject.Services.ExportProvider.GetExportedValueOrDefault<IAssetsFileDependenciesDataSource>(unconfiguredProject.Capabilities);
                IUnconfiguredProjectVsServices? projectVsServices = unconfiguredProject.Services.ExportProvider.GetExportedValue<IUnconfiguredProjectVsServices>();
                IProjectTree? dependenciesNode = projectVsServices?.ProjectTree.CurrentTree?.FindChildWithFlags(DependencyTreeFlags.DependenciesRootNode);

                if (dataSource == null || projectVsServices == null || dependenciesNode == null)
                {
                    // dataSource will be null for shared projects, for example
                    return;
                }

                AssetsFileDependenciesSnapshot snapshot = (await dataSource.GetLatestVersionAsync(unconfiguredProject.Services.DataSourceRegistry, cancellationToken: context.CancellationToken)).Value;

                foreach ((string target, AssetsFileTarget data) in snapshot.DataByTarget)
                {
                    IProjectTree subtreeNode;
                    if (snapshot.DataByTarget.Count > 1)
                    {
                        IProjectTree? targetNode = dependenciesNode.FindChildWithFlags(ProjectTreeFlags.Create("$TFM:" + target));

                        if (targetNode == null)
                        {
                            // TODO have seen this -- a race condition? can we prevent via sync link?
                            System.Diagnostics.Debug.Fail("Should not fail to find the target node.");

                            // If we cannot find the target node for this library, we won't find it for others in the same target.
                            continue;
                        }

                        subtreeNode = targetNode;
                    }
                    else
                    {
                        subtreeNode = dependenciesNode;
                    }

                    var itemByNameAndNodeType = new Dictionary<(string LibraryName, NodeType Type), object?>();

                    foreach ((_, AssetsFileTargetLibrary library) in data.LibraryByName)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            // Search was cancelled
                            return;
                        }

                        if (context.IsMatch(library.Name))
                        {
                            context.SubmitResult(GetOrCreateLibraryItem(library));
                        }

                        SearchAssemblies(library, library.CompileTimeAssemblies, PackageAssemblyGroupType.CompileTime);
                        SearchAssemblies(library, library.FrameworkAssemblies, PackageAssemblyGroupType.Framework);
                    }

                    foreach (AssetsFileLogMessage log in data.Logs)
                    {
                        if (context.IsMatch(log.Message))
                        {
                            context.SubmitResult(CreateLogItem(log));
                        }
                    }

                    return;

                    void SearchAssemblies(AssetsFileTargetLibrary library, ImmutableArray<string> assemblies, PackageAssemblyGroupType groupType)
                    {
                        List<string>? matches = null;

                        foreach (string assembly in assemblies)
                        {
                            if (context.IsMatch(Path.GetFileName(assembly)))
                            {
                                matches ??= new List<string>();
                                matches.Add(assembly);
                            }
                        }

                        if (matches != null)
                        {
                            var groupItem = PackageAssemblyGroupItem.CreateWithContainedByItems(snapshot, library, groupType, containedByItems: new[] { GetOrCreateLibraryItem(library) });

                            foreach (string match in matches)
                            {
                                context.SubmitResult(new PackageAssemblyItem(match, groupItem));
                            }
                        }
                    }

                    object? GetOrCreateLibraryItem(AssetsFileTargetLibrary library)
                    {
                        NodeType libraryType = library.Type switch
                        {
                            AssetsFileLibraryType.Package => NodeType.Package,
                            AssetsFileLibraryType.Project => NodeType.Project,
                            _ => throw Assumes.NotReachable()
                        };

                        (string Name, NodeType Package) key = (library.Name, libraryType);

                        if (!itemByNameAndNodeType.TryGetValue(key, out object? item))
                        {
                            if (data.IsTopLevel(library))
                            {
                                // Top level dependencies are special in that they anchor the nodes we
                                // create during a search operation to existing hierarchy tree items.
                                // Those hierarchy items are created via IProjectTree using evaluation data.
                                if (TryGetHierarchyItem(out IVsHierarchyItem hierarchyItem))
                                {
                                    itemByNameAndNodeType[key] = item = hierarchyItem;
                                }
                            }
                            else
                            {
                                // Look up containing items recursively, and cache results
                                List<object>? containedByItems = FindContainedByItems(library);

                                if (containedByItems == null)
                                {
                                    itemByNameAndNodeType[key] = null;
                                }
                                else
                                {
                                    itemByNameAndNodeType[key] = item = library.Type switch
                                    {
                                        AssetsFileLibraryType.Package => PackageReferenceItem.CreateWithContainedByItems(snapshot, library, containedByItems),
                                        AssetsFileLibraryType.Project => ProjectReferenceItem.CreateWithContainedByItems(library, containedByItems),
                                        _ => throw Assumes.NotReachable()
                                    };
                                }
                            }
                        }

                        return item;

                        bool TryGetHierarchyItem(out IVsHierarchyItem hierarchyItem)
                        {
                            IProjectTree? typeGroupNode = library.Type switch
                            {
                                AssetsFileLibraryType.Package => subtreeNode.FindChildWithFlags(DependencyTreeFlags.PackageDependencyGroup),
                                AssetsFileLibraryType.Project => subtreeNode.FindChildWithFlags(DependencyTreeFlags.ProjectDependencyGroup),
                                _ => throw Assumes.NotReachable()
                            };

                            IProjectTree? libraryNode = typeGroupNode?.FindChildWithFlags(ProjectTreeFlags.Create("$ID:" + library.Name));

                            if (libraryNode == null)
                            {
                                System.Diagnostics.Debug.Fail("Could not find tree item for library: " + library.Name);
                                hierarchyItem = default!;
                                return false;
                            }

                            uint itemId = (uint)libraryNode.Identity.ToInt32();
                            hierarchyItem = _hierarchyItemManager.GetHierarchyItem(projectVsServices!.VsHierarchy, itemId);
                            return true;
                        }
                    }

                    object? CreateLogItem(AssetsFileLogMessage log)
                    {
                        if (data.LibraryByName.TryGetValue(log.LibraryId, out AssetsFileTargetLibrary library))
                        {
                            object? libraryItem = GetOrCreateLibraryItem(library);

                            if (libraryItem != null)
                            {
                                return DiagnosticItem.CreateWithContainedByItems(log, new[] { libraryItem });
                            }
                        }

                        return null;
                    }

                    List<object>? FindContainedByItems(AssetsFileTargetLibrary library)
                    {
                        List<object>? containedByItems = null;

                        // Find all dependents
                        if (data.TryGetDependents(library.Name, out ImmutableArray<AssetsFileTargetLibrary> dependents))
                        {
                            // Library has at least one other library that depends upon it. Add these to the 'contained by' collection.
                            foreach (AssetsFileTargetLibrary dependent in dependents)
                            {
                                object? dependentItem = GetOrCreateLibraryItem(dependent);

                                if (dependentItem != null)
                                {
                                    containedByItems ??= new List<object>();
                                    containedByItems.Add(dependentItem);
                                }
                            }
                        }

                        return containedByItems;
                    }
                }
            }
        }

        private enum NodeType
        {
            Project,
            Package
        }
    }
}
