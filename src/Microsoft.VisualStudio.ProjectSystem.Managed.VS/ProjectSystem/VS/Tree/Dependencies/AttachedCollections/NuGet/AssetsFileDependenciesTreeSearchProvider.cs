// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Implementation of <see cref="IDependenciesTreeSearchProvider"/> that searches an <see cref="AssetsFileDependenciesSnapshot"/>
    /// for items that match the search string.
    /// </summary>
    [Export(typeof(IDependenciesTreeSearchProvider))]
    internal sealed class AssetsFileDependenciesTreeSearchProvider : IDependenciesTreeSearchProvider
    {
        private readonly IFileIconProvider _fileIconProvider;

        [ImportingConstructor]
        public AssetsFileDependenciesTreeSearchProvider(IFileIconProvider fileIconProvider)
        {
            _fileIconProvider = fileIconProvider;
        }

        public async Task SearchAsync(IDependenciesTreeProjectSearchContext context)
        {
            // get latest snapshot
            // TODO GetExportedValueOrDefault is in VS.Utilities so we cannot use it here
            IAssetsFileDependenciesDataSource? dataSource = context.UnconfiguredProject.Services.ExportProvider.GetExportedValueOrDefault<IAssetsFileDependenciesDataSource>(context.UnconfiguredProject.Capabilities);

            if (dataSource == null)
            {
                // dataSource will be null for shared projects, for example
                return;
            }

            AssetsFileDependenciesSnapshot snapshot = (await dataSource.GetLatestVersionAsync(context.UnconfiguredProject.Services.DataSourceRegistry, cancellationToken: context.CancellationToken)).Value;

            foreach ((_, AssetsFileTarget target) in snapshot.DataByTarget)
            {
                IDependenciesTreeProjectTargetSearchContext? targetContext = context.ForTarget(target.Target);

                if (targetContext == null)
                {
                    continue;
                }

                foreach ((_, AssetsFileTargetLibrary library) in target.LibraryByName)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        // Search was cancelled
                        return;
                    }

                    if (targetContext.IsMatch(library.Name))
                    {
                        targetContext.SubmitResult(CreateLibraryItem(library));
                    }

                    SearchAssemblies(library, library.CompileTimeAssemblies, PackageAssemblyGroupType.CompileTime);
                    SearchAssemblies(library, library.FrameworkAssemblies, PackageAssemblyGroupType.Framework);
                    SearchContentFiles(library);
                }

                SearchLogMessages();

                return;

                void SearchAssemblies(AssetsFileTargetLibrary library, ImmutableArray<string> assemblies, PackageAssemblyGroupType groupType)
                {
                    foreach (string assembly in assemblies)
                    {
                        if (targetContext.IsMatch(Path.GetFileName(assembly)))
                        {
                            targetContext.SubmitResult(new PackageAssemblyItem(target, library, assembly, groupType));
                        }
                    }
                }

                void SearchContentFiles(AssetsFileTargetLibrary library)
                {
                    foreach (AssetsFileTargetLibraryContentFile contentFile in library.ContentFiles)
                    {
                        if (targetContext.IsMatch(contentFile.Path))
                        {
                            targetContext.SubmitResult(new PackageContentFileItem(target, library, contentFile, _fileIconProvider));
                        }
                    }
                }

                IRelatableItem CreateLibraryItem(AssetsFileTargetLibrary library)
                {
                    return library.Type switch
                    {
                        AssetsFileLibraryType.Package => new PackageReferenceItem(target, library),
                        AssetsFileLibraryType.Project => new ProjectReferenceItem(target, library),
                        _ => throw Assumes.NotReachable()
                    };
                }

                void SearchLogMessages()
                {
                    foreach (AssetsFileLogMessage log in target.Logs)
                    {
                        if (targetContext.IsMatch(log.Message))
                        {
                            targetContext.SubmitResult(CreateLogItem(log));
                        }
                    }

                    DiagnosticItem? CreateLogItem(AssetsFileLogMessage log)
                    {
                        if (target.LibraryByName.TryGetValue(log.LibraryId, out AssetsFileTargetLibrary library))
                        {
                            return new DiagnosticItem(target, library, log);
                        }

                        return null;
                    }
                }
            }
        }
    }
}
