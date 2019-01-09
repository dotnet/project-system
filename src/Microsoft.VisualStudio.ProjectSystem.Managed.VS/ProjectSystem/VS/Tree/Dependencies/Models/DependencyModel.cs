// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal abstract class DependencyModel : IDependencyModel
    {
        [Flags]
        private enum DependencyFlags : byte
        {
            Resolved = 1 << 0,
            TopLevel = 1 << 1,
            Implicit = 1 << 2,
            Visible = 1 << 3
        }

        /// <summary>
        /// Assists in the creation and caching of flags used by <see cref="DependencyModel"/> subclasses.
        /// </summary>
        /// <remarks>
        /// <see cref="ProjectTreeFlags"/> internally performs operations on immutable sets during operations such as
        /// <see cref="ProjectTreeFlags.Union(ProjectTreeFlags)"/> and <see cref="ProjectTreeFlags.Union(ProjectTreeFlags)"/>
        /// which commonly results in allocating identical values on the heap. By caching them, dependency model types can
        /// avoid such allocations during their construction, keeping them lighter.
        /// </remarks>
        protected readonly struct DependencyFlagCache
        {
            private readonly ProjectTreeFlags[] _lookup;

            public DependencyFlagCache(ProjectTreeFlags add = default, ProjectTreeFlags remove = default)
            {
                // The 'isResolved' dimension determines whether we start with generic resolved or unresolved dependency flags.
                // We then add (union) and remove (except) any other flags as instructed.

                ProjectTreeFlags resolved = DependencyTreeFlags.GenericResolvedDependencyFlags.Union(add).Except(remove);
                ProjectTreeFlags unresolved = DependencyTreeFlags.GenericUnresolvedDependencyFlags.Union(add).Except(remove);

                // The 'isImplicit' dimension only enforces, when true, that the dependency cannot be removed.

                _lookup = new ProjectTreeFlags[4];
                _lookup[Index(isResolved: true, isImplicit: false)] = resolved;
                _lookup[Index(isResolved: true, isImplicit: true)] = resolved.Except(DependencyTreeFlags.SupportsRemove);
                _lookup[Index(isResolved: false, isImplicit: false)] = unresolved;
                _lookup[Index(isResolved: false, isImplicit: true)] = unresolved.Except(DependencyTreeFlags.SupportsRemove);
            }

            /// <summary>Retrieves the cached <see cref="ProjectTreeFlags"/> given the arguments.</summary>
            public ProjectTreeFlags Get(bool isResolved, bool isImplicit) => _lookup[Index(isResolved, isImplicit)];

            /// <summary>Provides a unique mapping between (bool,bool) and [0,3].</summary>
            private static int Index(bool isResolved, bool isImplicit) => (isResolved ? 2 : 0) | (isImplicit ? 1 : 0);
        }

        protected DependencyModel(
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties,
            bool isTopLevel = true,
            bool isVisible = true)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            Path = path;
            OriginalItemSpec = originalItemSpec ?? path;
            Properties = properties ?? ImmutableStringDictionary<string>.EmptyOrdinal;
            Caption = path;
            Flags = flags;

            if (Properties.TryGetBoolProperty("Visible", out bool visibleProperty))
            {
                isVisible = visibleProperty;
            }

            DependencyFlags depFlags = 0;
            if (isResolved)
                depFlags |= DependencyFlags.Resolved;
            if (isVisible)
                depFlags |= DependencyFlags.Visible;
            if (isImplicit)
                depFlags |= DependencyFlags.Implicit;
            if (isTopLevel)
                depFlags |= DependencyFlags.TopLevel;
            _flags = depFlags;
        }

        private readonly DependencyFlags _flags;

        public abstract string ProviderType { get; }

        public virtual string Name => Path;
        public string Caption { get; protected set; }
        public string OriginalItemSpec { get; }
        public string Path { get; }
        public virtual string SchemaName => null;
        public virtual string SchemaItemType => null;
        public virtual string Version => null;
        public bool Resolved => (_flags & DependencyFlags.Resolved) != 0;
        public bool TopLevel => (_flags & DependencyFlags.TopLevel) != 0;
        public bool Implicit => (_flags & DependencyFlags.Implicit) != 0;
        public bool Visible => (_flags & DependencyFlags.Visible) != 0;
        public virtual int Priority => 0;
        public ImageMoniker Icon => IconSet.Icon;
        public ImageMoniker ExpandedIcon => IconSet.ExpandedIcon;
        public ImageMoniker UnresolvedIcon => IconSet.UnresolvedIcon;
        public ImageMoniker UnresolvedExpandedIcon => IconSet.UnresolvedExpandedIcon;
        public IImmutableDictionary<string, string> Properties { get; }
        public virtual IImmutableList<string> DependencyIDs => ImmutableList<string>.Empty;
        public ProjectTreeFlags Flags { get; }

        public abstract DependencyIconSet IconSet { get; }

        public string Id => OriginalItemSpec;

        public override int GetHashCode()
        {
            return unchecked(
                StringComparer.OrdinalIgnoreCase.GetHashCode(Id) +
                StringComparers.DependencyProviderTypes.GetHashCode(ProviderType));
        }

        public override bool Equals(object obj)
        {
            return obj is IDependencyModel other && Equals(other);
        }

        public bool Equals(IDependencyModel other)
        {
            return other != null
                && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase)
                && StringComparers.DependencyProviderTypes.Equals(other.ProviderType, ProviderType);
        }

        public override string ToString() => Id;
    }
}
