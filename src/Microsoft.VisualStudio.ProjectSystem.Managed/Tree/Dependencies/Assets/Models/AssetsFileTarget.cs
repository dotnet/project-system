// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Assets.Models
{
    /// <summary>
    /// Immutable snapshot of data captured from <c>project.assets.json</c> that relates to a specific target.
    /// </summary>
    internal sealed class AssetsFileTarget
    {
        /// <summary>
        /// Gets the target name, such as <c>.NETFramework,Version=v4.8</c> or <c>.NETStandard,Version=v1.3</c>.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Gets diagnostic messages for this target. Often empty.
        /// </summary>
        public ImmutableArray<AssetsFileLogMessage> Logs { get; }

        /// <summary>
        /// Stores data about libraries (packages/projects), keyed by name.
        /// </summary>
        public ImmutableDictionary<string, AssetsFileTargetLibrary> LibraryByName { get; }

        /// <summary>
        /// Gets the names of the top-level dependencies of this target.
        /// </summary>
        private readonly HashSet<string>? _topLevelDependencies;

        /// <summary>
        /// Lazily populated cache of back-references in the dependencies graph, mapping libraries to their ancestor(s).
        /// Created by <see cref="TryGetDependents"/>, which is used during computation of Solution Explorer search results.
        /// </summary>
        private Dictionary<string, ImmutableArray<AssetsFileTargetLibrary>>? _dependentsByLibrary;

        public AssetsFileTarget(string target, HashSet<string>? topLevelDependencies, ImmutableArray<AssetsFileLogMessage> logs, ImmutableDictionary<string, AssetsFileTargetLibrary> libraryByName)
        {
            Requires.NotNullOrWhiteSpace(target, nameof(target));
            Requires.Argument(!logs.IsDefault, nameof(logs), "Must not be default");
            Requires.NotNull(libraryByName, nameof(libraryByName));
            
            Target = target;
            _topLevelDependencies = topLevelDependencies;
            Logs = logs;
            LibraryByName = libraryByName;
        }

        /// <summary>
        /// Gets whether <paramref name="library"/> is a top-level dependency of the project.
        /// </summary>
        public bool IsTopLevel(AssetsFileTargetLibrary library) => _topLevelDependencies != null && _topLevelDependencies.Contains(library.Name);

        /// <summary>
        /// Gets the set of dependents (parents) of <paramref name="libraryName"/>.
        /// </summary>
        /// <returns><see langword="true"/> if dependents were found, otherwise <see langword="false"/>.</returns>
        public bool TryGetDependents(string libraryName, out ImmutableArray<AssetsFileTargetLibrary> dependents)
        {
            // Defer construction of dependents collection until needed. It's only needed for Solution Explorer search.
            if (_dependentsByLibrary == null)
            {
                var dependentsByLibrary = new Dictionary<string, ImmutableArray<AssetsFileTargetLibrary>.Builder>(LibraryByName.Count);

                foreach ((_, AssetsFileTargetLibrary library) in LibraryByName)
                {
                    foreach (string dependency in library.Dependencies)
                    {
                        GetBuilder(dependency).Add(library);
                    }
                }

                _dependentsByLibrary = dependentsByLibrary.ToDictionary(pair => pair.Key, pair => pair.Value.ToImmutable());

                ImmutableArray<AssetsFileTargetLibrary>.Builder GetBuilder(string library)
                {
                    if (!dependentsByLibrary.TryGetValue(library, out ImmutableArray<AssetsFileTargetLibrary>.Builder builder))
                    {
                        dependentsByLibrary[library] = builder = ImmutableArray.CreateBuilder<AssetsFileTargetLibrary>();
                    }

                    return builder;
                }
            }

            return _dependentsByLibrary.TryGetValue(libraryName, out dependents);
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            s.Append("Target \"").Append(Target).Append("\" ");
            s.Append(LibraryByName.Count).Append(LibraryByName.Count == 1 ? " library (" : " libraries (");
            s.Append(_topLevelDependencies?.Count ?? 0).Append(" top level) ");
            s.Append(Logs.Length).Append(Logs.Length == 1 ? " log" : " logs");
            return s.ToString();
        }
    }
}
