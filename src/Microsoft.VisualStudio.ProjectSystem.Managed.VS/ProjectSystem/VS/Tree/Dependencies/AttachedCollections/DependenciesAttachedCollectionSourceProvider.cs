// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Identifies which hierarchy items in the "Dependencies" tree should have collections of child
    /// items attached, and provides source objects for those collections.
    /// </summary>
    [Export(typeof(IAttachedCollectionSourceProvider))]
    [Name(nameof(DependenciesAttachedCollectionSourceProvider))]
    [VisualStudio.Utilities.Order(Before = HierarchyItemsProviderNames.Contains)]
    internal sealed class DependenciesAttachedCollectionSourceProvider : IAttachedCollectionSourceProvider
    {
        private readonly HierarchyItemFlagsDetector _packageReferenceTest = new HierarchyItemFlagsDetector(DependencyTreeFlags.PackageDependency);
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportingConstructor]
        public DependenciesAttachedCollectionSourceProvider(JoinableTaskContext joinableTaskContext)
        {
            _joinableTaskContext = joinableTaskContext;
        }

        public IAttachedCollectionSource? CreateCollectionSource(object item, string relationshipName)
        {
            if (relationshipName == KnownRelationships.Contains)
            {
                if (item is IVsHierarchyItem hierarchyItem)
                {
                    if (_packageReferenceTest.Matches(hierarchyItem))
                    {
                        // This is a package reference
                        return CreatePackageReferenceSource(hierarchyItem, _joinableTaskContext);
                    }
                }
            }

            return null;

            static IAttachedCollectionSource? CreatePackageReferenceSource(IVsHierarchyItem hierarchyItem, JoinableTaskContext joinableTaskContext)
            {
                UnconfiguredProject? unconfiguredProject = hierarchyItem.HierarchyIdentity.Hierarchy.AsUnconfiguredProject();
                
                if (unconfiguredProject == null)
                {
                    return null;
                }

                // Determine the package's ID and version
                if (!hierarchyItem.TryGetPackageDetails(out string? packageId, out string? packageVersion))
                {
                    return null;
                }

                // Find the data source
                IAssetsFileDependenciesDataSource? dataSource = unconfiguredProject.Services.ExportProvider.GetExportedValueOrDefault<IAssetsFileDependenciesDataSource>();

                if (dataSource != null)
                {
                    // Configuration will be null if project is not multi-targeting
                    hierarchyItem.TryFindConfiguration(out string? configuration);

                    return new PackageReferenceAttachedCollectionSource(hierarchyItem, configuration, packageId, packageVersion, dataSource, joinableTaskContext);
                }

                return null;
            }
        }

        public IEnumerable<IAttachedRelationship> GetRelationships(object item) => Enumerable.Empty<IAttachedRelationship>();
    }
}
