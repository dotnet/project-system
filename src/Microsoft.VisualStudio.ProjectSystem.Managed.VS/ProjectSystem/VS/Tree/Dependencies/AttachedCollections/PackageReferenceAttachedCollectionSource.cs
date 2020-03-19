// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    internal sealed class PackageReferenceAttachedCollectionSource : IAsyncAttachedCollectionSource
    {
        private static readonly PropertyChangedEventArgs s_hasItemsPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(HasItems));
        private static readonly PropertyChangedEventArgs s_isUpdatingItemsPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsUpdatingHasItems));

        public event PropertyChangedEventHandler? PropertyChanged;
        
        private readonly IVsHierarchyItem _hierarchyItem;
        private ReplaceableCollection? _items;

        public PackageReferenceAttachedCollectionSource(
            IVsHierarchyItem hierarchyItem,
            string? configuration,
            string packageId,
            string version,
            IAssetsFileDependenciesDataSource dataSource,
            JoinableTaskContext joinableTaskContext)
        {
            _hierarchyItem = hierarchyItem;

            IsUpdatingHasItems = true;

            IDisposable link = dataSource.SourceBlock.LinkToAsyncAction(async versionedValue =>
            {
                AssetsFileDependenciesSnapshot snapshot = versionedValue.Value;

                List<object>? items = null;
                if (!snapshot.Logs.IsEmpty)
                {
                    foreach (AssetsFileLogMessage log in snapshot.Logs)
                    {
                        if (log.LibraryId == packageId && (configuration == null || log.TargetGraphs.Any(target => string.Equals(configuration, target))))
                        {
                            items ??= new List<object>();
                            items.Add(new DiagnosticItem(log));
                        }
                    }
                }

                await joinableTaskContext.Factory.SwitchToMainThreadAsync();

                if (items != null)
                {
                    if (_items != null)
                    {
                        _items.Replace(this, items);
                    }
                    else
                    {
                        _items = new ReplaceableCollection(items);
                        PropertyChanged?.Invoke(this, s_hasItemsPropertyChangedEventArgs);
                    }
                }
                else if (_items != null)
                {
                    _items = null;
                    PropertyChanged?.Invoke(this, s_hasItemsPropertyChangedEventArgs);
                }

                if (IsUpdatingHasItems)
                {
                    IsUpdatingHasItems = false;
                    PropertyChanged?.Invoke(this, s_isUpdatingItemsPropertyChangedEventArgs);
                }
            });

            Assumes.False(hierarchyItem.IsDisposed);
            hierarchyItem.PropertyChanged += OnItemPropertyChanged;

            void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed) && hierarchyItem.IsDisposed)
                {
                    link.Dispose();
                    hierarchyItem.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        }

        public bool HasItems => _items != null;

        public IEnumerable? Items => _items;

        public object SourceItem => _hierarchyItem;

        public bool IsUpdatingHasItems { get; private set; }

        /// <summary>
        /// Unfortunately, raising <see cref="INotifyPropertyChanged"/> for <see cref="Items"/> doesn't trigger a refresh.
        /// We must use <see cref="INotifyCollectionChanged"/> instead.
        /// </summary>
        private sealed class ReplaceableCollection : IEnumerable, INotifyCollectionChanged
        {
            private static readonly NotifyCollectionChangedEventArgs s_resetCollectionChangedEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            private IEnumerable _enumerable;

            public ReplaceableCollection(IEnumerable enumerable) => _enumerable = enumerable;

            public void Replace(object source, IEnumerable enumerable)
            {
                _enumerable = enumerable;
                CollectionChanged?.Invoke(source, s_resetCollectionChangedEventArgs);
            }

            public IEnumerator GetEnumerator() => _enumerable.GetEnumerator();
        }
    }
}
