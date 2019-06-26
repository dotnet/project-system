// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal abstract partial class DependencyModel
    {
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
    }
}
