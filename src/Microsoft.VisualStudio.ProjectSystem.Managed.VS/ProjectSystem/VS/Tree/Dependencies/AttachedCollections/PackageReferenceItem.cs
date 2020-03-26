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
    /// Backing object for transitive package reference nodes in the dependencies tree.
    /// </summary>
    internal sealed class PackageReferenceItem : AttachedCollectionItemBase, IContainsAttachedItems, IAttachedCollectionSource
    {
        private readonly AssetsFileTargetLibrary _library;
        private readonly string? _configuration;
        private readonly AssetsFileDependenciesSnapshot _snapshot;
        private IEnumerable? _items;

        public PackageReferenceItem(AssetsFileTargetLibrary library, string? configuration, AssetsFileDependenciesSnapshot snapshot)
            : base($"{library.Name} ({library.Version})")
        {
            _library = library;
            _configuration = configuration;
            _snapshot = snapshot;
        }

        public override int Priority => AttachedItemPriority.Package;

        public override ImageMoniker IconMoniker => ManagedImageMonikers.NuGetGrey;

        public override ImageMoniker ExpandedIconMoniker => IconMoniker;

        public override object? GetBrowseObject() => new BrowseObject(_library, _snapshot);

        public IAttachedCollectionSource ContainsAttachedCollectionSource => this;

        bool IAttachedCollectionSource.HasItems => !_library.Dependencies.IsEmpty || !_library.CompileTimeAssemblies.IsEmpty;

        IEnumerable IAttachedCollectionSource.Items
        {
            get
            {
                // NOTE any change to the construction of these items must be reflected in the implementation of HasItems
                if (_items == null)
                {
                    if (_snapshot.TryGetDependencies(_library.Name, _library.Version, _configuration, out ImmutableArray<AssetsFileTargetLibrary> dependencies))
                    {
                        int length = _library.CompileTimeAssemblies.IsEmpty ? dependencies.Length : dependencies.Length + 1;
                        
                        ImmutableArray<object>.Builder builder = ImmutableArray.CreateBuilder<object>(length);
                        builder.AddRange(dependencies.Select(dep => new PackageReferenceItem(dep, _configuration, _snapshot)));
                        
                        if (!_library.CompileTimeAssemblies.IsEmpty)
                        {
                            builder.Add(new PackageAssemblyGroupItem(PackageAssemblyGroupType.CompileTime, _library.CompileTimeAssemblies));
                        }

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
            private readonly AssetsFileDependenciesSnapshot _snapshot;

            public BrowseObject(AssetsFileTargetLibrary library, AssetsFileDependenciesSnapshot snapshot)
            {
                _library = library;
                _snapshot = snapshot;
            }

            public override string GetComponentName() => $"{_library.Name} ({_library.Version})";

            public override string GetClassName() => VSResources.PackageReferenceBrowseObjectClassName;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferenceNameDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferenceNameDescription))]
            public string Name => _library.Name;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferenceVersionDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferenceVersionDescription))]
            public string Version => _library.Version;

            [BrowseObjectDisplayName(nameof(VSResources.PackageReferencePathDisplayName))]
            [BrowseObjectDescription(nameof(VSResources.PackageReferencePathDescription))]
            public string? Path => _snapshot.TryResolvePackagePath(_library.Name, _library.Version, out string? fullPath) ? fullPath : null;
        }
    }
}
