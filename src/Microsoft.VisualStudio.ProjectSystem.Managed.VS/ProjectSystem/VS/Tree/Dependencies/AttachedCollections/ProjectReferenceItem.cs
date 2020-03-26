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
    internal sealed class ProjectReferenceItem : AttachedCollectionItemBase, IContainsAttachedItems, IAttachedCollectionSource
    {
        private readonly AssetsFileTargetLibrary _library;
        private readonly string? _configuration;
        private readonly AssetsFileDependenciesSnapshot _snapshot;
        private IEnumerable? _items;

        public ProjectReferenceItem(AssetsFileTargetLibrary library, string? configuration, AssetsFileDependenciesSnapshot snapshot)
            : base(library.Name)
        {
            _library = library;
            _configuration = configuration;
            _snapshot = snapshot;
        }

        public override int Priority => AttachedItemPriority.Project;

        public override ImageMoniker IconMoniker => ManagedImageMonikers.Application;

        public override ImageMoniker ExpandedIconMoniker => IconMoniker;

        public override object? GetBrowseObject() => new BrowseObject(_library);

        public IAttachedCollectionSource ContainsAttachedCollectionSource => this;

        bool IAttachedCollectionSource.HasItems => !_library.Dependencies.IsEmpty;

        IEnumerable IAttachedCollectionSource.Items
        {
            get
            {
                // NOTE any change to the construction of these items must be reflected in the implementation of HasItems
                if (_items == null)
                {
                    if (_snapshot.TryGetDependencies(_library.Name, _library.Version, _configuration, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
                    {
                        ImmutableArray<object>.Builder builder = ImmutableArray.CreateBuilder<object>(dependencies.Length);
                        builder.AddRange(dependencies.Select(dep => new PackageReferenceItem(dep, _configuration, _snapshot)));
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

        object? IAttachedCollectionSource.SourceItem => null;

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
