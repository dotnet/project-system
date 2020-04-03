// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// Backing object for transitive project reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class ProjectReferenceItem : AttachedCollectionItemBase, IContainsAttachedItems, IContainedByAttachedItems
    {
        private readonly AssetsFileTargetLibrary _library;

        public static ProjectReferenceItem CreateWithContainsItems(AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, string? target)
        {
            var item = new ProjectReferenceItem(library);
            item.ContainsAttachedCollectionSource = new ContainsCollectionSource(item, snapshot, library, target);
            return item;
        }

        public static ProjectReferenceItem CreateWithContainedByItems(AssetsFileTargetLibrary library, IEnumerable containedByItems)
        {
            var item = new ProjectReferenceItem(library);
            item.ContainedByAttachedCollectionSource = new MaterializedAttachedCollectionSource(item, containedByItems);
            return item;
        }

        private ProjectReferenceItem(AssetsFileTargetLibrary library)
            : base(library.Name)
        {
            _library = library;
        }

        public override int Priority => AttachedItemPriority.Project;

        public override ImageMoniker IconMoniker => ManagedImageMonikers.Application;

        public override ImageMoniker ExpandedIconMoniker => IconMoniker;

        public override object? GetBrowseObject() => new BrowseObject(_library);

        public IAttachedCollectionSource? ContainsAttachedCollectionSource { get; private set; }

        public IAttachedCollectionSource? ContainedByAttachedCollectionSource { get; private set; }

        private sealed class ContainsCollectionSource : IAttachedCollectionSource
        {
            private readonly AssetsFileDependenciesSnapshot _snapshot;
            private readonly AssetsFileTargetLibrary _library;
            private readonly string? _target;
            private IEnumerable? _items;

            public ContainsCollectionSource(ProjectReferenceItem sourceItem, AssetsFileDependenciesSnapshot snapshot, AssetsFileTargetLibrary library, string? target)
            {
                SourceItem = sourceItem;
                _snapshot = snapshot;
                _library = library;
                _target = target;
            }

            public object? SourceItem { get; }

            public bool HasItems => !_library.Dependencies.IsEmpty;

            public IEnumerable Items
            {
                get
                {
                    // NOTE any change to the construction of these items must be reflected in the implementation of HasItems
                    if (_items == null)
                    {
                        if (_snapshot.TryGetDependencies(_library.Name, _library.Version, _target, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
                        {
                            ImmutableArray<object>.Builder builder = ImmutableArray.CreateBuilder<object>(dependencies.Length);
                            builder.AddRange(dependencies.Select(dep => PackageReferenceItem.CreateWithContainsItems(_snapshot, dep, _target)));
                            _items = builder.MoveToImmutable();
                        }
                        else
                        {
                            _items = Enumerable.Empty<object>();
                        }
                    }

                    return _items;
                }
            }
        }

        private sealed class BrowseObject : BrowseObjectBase
        {
            private readonly AssetsFileTargetLibrary _library;

            public BrowseObject(AssetsFileTargetLibrary library) => _library = library;

            public override string GetComponentName() => _library.Name;

            public override string GetClassName() => VSResources.ProjectReferenceBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.ProjectReferenceNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.ProjectReferenceNameDescription))]
            public string Name => _library.Name;
        }
    }
}
