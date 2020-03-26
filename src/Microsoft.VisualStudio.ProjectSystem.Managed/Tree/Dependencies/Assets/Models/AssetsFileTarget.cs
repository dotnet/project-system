// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

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

        public AssetsFileTarget(string target, ImmutableArray<AssetsFileLogMessage> logs, ImmutableDictionary<string, AssetsFileTargetLibrary> libraryByName)
        {
            Requires.NotNullOrWhiteSpace(target, nameof(target));
            Requires.Argument(!logs.IsDefault, nameof(logs), "Must not be default");
            Requires.NotNull(libraryByName, nameof(libraryByName));
            
            Target = target;
            Logs = logs;
            LibraryByName = libraryByName;
        }
    }
}
