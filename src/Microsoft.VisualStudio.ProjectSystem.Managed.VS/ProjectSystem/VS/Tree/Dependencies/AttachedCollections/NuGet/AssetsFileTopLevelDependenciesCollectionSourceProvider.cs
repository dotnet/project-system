// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.NuGet.Models;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Base class for attaching children to top-level dependencies that come from the assets file.
    /// </summary>
    /// <remarks>
    /// Templates out common code with a bunch of protected methods to override for specific item types.
    /// </remarks>
    internal abstract class AssetsFileTopLevelDependenciesCollectionSourceProvider<TIdentity, TItem> : DependenciesAttachedCollectionSourceProviderBase
        where TItem : class, IRelatableItem
    {
        protected AssetsFileTopLevelDependenciesCollectionSourceProvider(ProjectTreeFlags flags)
            : base(flags)
        {
        }

        protected abstract bool TryGetIdentity(string flagsString, out TIdentity identity);

        protected abstract bool TryGetLibrary(AssetsFileTarget target, TIdentity identity, [NotNullWhen(returnValue: true)] out AssetsFileTargetLibrary? library);

        protected abstract TItem CreateItem(AssetsFileTarget targetData, AssetsFileTargetLibrary library);

        protected abstract bool TryUpdateItem(TItem item, AssetsFileTarget targetData, AssetsFileTargetLibrary library);

        protected override bool TryCreateCollectionSource(
            IVsHierarchyItem hierarchyItem,
            string flagsString,
            string? target,
            IRelationProvider relationProvider,
            [NotNullWhen(returnValue: true)] out AggregateRelationCollectionSource? containsCollectionSource)
        {
            if (!TryGetIdentity(flagsString, out TIdentity identity))
            {
                containsCollectionSource = null;
                return false;
            }

            UnconfiguredProject? unconfiguredProject = hierarchyItem.HierarchyIdentity.Hierarchy.AsUnconfiguredProject();

            // Find the data source
            IAssetsFileDependenciesDataSource? dataSource = unconfiguredProject?.Services.ExportProvider.GetExportedValueOrDefault<IAssetsFileDependenciesDataSource>();

            if (unconfiguredProject == null || dataSource == null)
            {
                containsCollectionSource = null;
                return false;
            }

            IProjectThreadingService projectThreadingService = unconfiguredProject.Services.ThreadingPolicy;

            projectThreadingService.VerifyOnUIThread();

            // Items for top-level dependencies do not appear in the tree directly. They "shadow" hierarchy items
            // for purposes of bridging between the hierarchy items (top-level dependencies) and attached items (transitive
            // dependencies).
            TItem? item = null;

            var collectionSource = new AggregateRelationCollectionSource(hierarchyItem);
            AggregateContainsRelationCollection? collection = null;

            IDisposable link = dataSource.SourceBlock.LinkToAsyncAction(
                async versionedValue =>
                {
                    AssetsFileDependenciesSnapshot snapshot = versionedValue.Value;
                    if (snapshot.TryGetTarget(target, out AssetsFileTarget? targetData))
                    {
                        if (TryGetLibrary(targetData, identity, out AssetsFileTargetLibrary? library))
                        {
                            if (item == null)
                            {
                                // This is the first update
                                item = CreateItem(targetData, library);
                                if (AggregateContainsRelationCollection.TryCreate(item, relationProvider, out collection))
                                {
                                    await projectThreadingService.JoinableTaskContext.Factory.SwitchToMainThreadAsync();
                                    collectionSource.SetCollection(collection);
                                }
                            }
                            else if (TryUpdateItem(item, targetData, library) && collection != null)
                            {
                                await projectThreadingService.JoinableTaskContext.Factory.SwitchToMainThreadAsync();
                                collection.OnStateUpdated();
                            }
                        }
                    }
                },
                unconfiguredProject);

            Assumes.False(hierarchyItem.IsDisposed);
            hierarchyItem.PropertyChanged += OnItemPropertyChanged;

            void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                // We are notified when the IVsHierarchyItem is removed from the tree via its INotifyPropertyChanged
                // event for the IsDisposed property. When this fires, we dispose our dataflow link and release the
                // subscription.
                if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed) && hierarchyItem.IsDisposed)
                {
                    link.Dispose();
                    hierarchyItem.PropertyChanged -= OnItemPropertyChanged;
                }
            }

            containsCollectionSource = collectionSource;
            return true;
        }
    }
}
