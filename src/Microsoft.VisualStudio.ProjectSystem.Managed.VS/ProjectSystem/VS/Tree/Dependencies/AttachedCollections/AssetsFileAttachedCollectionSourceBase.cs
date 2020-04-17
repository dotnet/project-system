// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Base class for <see cref="IAttachedCollectionSource"/> implementations within the
    /// dependencies tree that subscribe to assets file data.
    /// </summary>
    internal abstract class AssetsFileAttachedCollectionSourceBase : IAsyncAttachedCollectionSource
    {
        private static readonly PropertyChangedEventArgs s_hasItemsPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(HasItems));
        private static readonly PropertyChangedEventArgs s_isUpdatingItemsPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsUpdatingHasItems));

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IVsHierarchyItem _hierarchyItem;
        private readonly IFileIconProvider _fileIconProvider;
        private ReplaceableCollection? _items;

        public bool IsUpdatingHasItems { get; private set; }

        public bool HasItems => _items != null;
        public IEnumerable? Items => _items;
        public object SourceItem => _hierarchyItem;

        protected AssetsFileAttachedCollectionSourceBase(UnconfiguredProject unconfiguredProject, IVsHierarchyItem hierarchyItem, IAssetsFileDependenciesDataSource dataSource, JoinableTaskContext joinableTaskContext, IFileIconProvider fileIconProvider)
        {
            Requires.NotNull(hierarchyItem, nameof(hierarchyItem));
            Requires.NotNull(dataSource, nameof(dataSource));
            Requires.NotNull(joinableTaskContext, nameof(joinableTaskContext));

            _hierarchyItem = hierarchyItem;
            _fileIconProvider = fileIconProvider;

            IsUpdatingHasItems = true;

            IDisposable link = dataSource.SourceBlock.LinkToAsyncAction(
                async versionedValue =>
                {
                    AssetsFileDependenciesSnapshot snapshot = versionedValue.Value;

                    IEnumerable<object>? items = UpdateItems(snapshot);

                    // TODO prevent switch if not needed
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
                },
                unconfiguredProject);

            Assumes.False(hierarchyItem.IsDisposed);
            hierarchyItem.PropertyChanged += OnItemPropertyChanged;

            void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                // We are notified when the IVsHierarchyItem is removed from the tree via its INotifyPropertyChanged
                // event, for the IsDisposed property. When this fires, we dispose our dataflow link and release the
                // subscription.
                if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed) && hierarchyItem.IsDisposed)
                {
                    link.Dispose();
                    hierarchyItem.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        }

        protected abstract IEnumerable<object>? UpdateItems(AssetsFileDependenciesSnapshot snapshot);

        protected static void ProcessLogMessages(ref List<object>? items, AssetsFileDependenciesSnapshot snapshot, string? target, string libraryId)
        {
            if (snapshot.TryGetLogMessages(target, out ImmutableArray<AssetsFileLogMessage> logMessages))
            {
                foreach (AssetsFileLogMessage log in logMessages)
                {
                    if (log.LibraryId == libraryId)
                    {
                        items ??= new List<object>();
                        items.Add(DiagnosticItem.Create(log));
                    }
                }
            }
        }

        protected void ProcessLibraryReferences(ref List<object>? items, AssetsFileDependenciesSnapshot snapshot, in ImmutableArray<AssetsFileTargetLibrary> dependencies, string? target)
        {
            if (!dependencies.IsEmpty)
            {
                items ??= new List<object>();
                foreach (AssetsFileTargetLibrary dependency in dependencies)
                {
                    items.Add(
                        dependency.Type switch
                        {
                            AssetsFileLibraryType.Package => PackageReferenceItem.CreateWithContainsItems(snapshot, dependency, target, _fileIconProvider),
                            AssetsFileLibraryType.Project => ProjectReferenceItem.CreateWithContainsItems(snapshot, dependency, target, _fileIconProvider),
                            _ => throw Assumes.NotReachable()
                        });
                }
            }
        }

        protected void ProcessLibraryContent(ref List<object>? items, AssetsFileTargetLibrary library, AssetsFileDependenciesSnapshot snapshot)
        {
            if (!library.CompileTimeAssemblies.IsEmpty)
            {
                items ??= new List<object>();
                items.Add(PackageAssemblyGroupItem.CreateWithContainsItems(snapshot, library, PackageAssemblyGroupType.CompileTime, library.CompileTimeAssemblies));
            }

            if (!library.FrameworkAssemblies.IsEmpty)
            {
                items ??= new List<object>();
                items.Add(PackageAssemblyGroupItem.CreateWithContainsItems(snapshot, library, PackageAssemblyGroupType.Framework, library.FrameworkAssemblies));
            }

            if (!library.ContentFiles.IsEmpty)
            {
                items ??= new List<object>();
                items.Add(PackageContentFilesGroupItem.CreateWithContainsItems(_fileIconProvider, library.ContentFiles));
            }
        }

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
