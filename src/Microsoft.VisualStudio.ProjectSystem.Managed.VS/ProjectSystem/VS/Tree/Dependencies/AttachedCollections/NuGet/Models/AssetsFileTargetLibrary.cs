// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGet.ProjectModel;

namespace Microsoft.VisualStudio.NuGet.Models
{
    /// <summary>
    /// Data about a library (package/project) in a given target, from <c>project.assets.json</c>. Immutable.
    /// </summary>
    internal sealed class AssetsFileTargetLibrary
    {
        public static bool TryCreate(LockFileTargetLibrary lockFileLibrary, [NotNullWhen(returnValue: true)] out AssetsFileTargetLibrary? library)
        {
            AssetsFileLibraryType type;
            if (lockFileLibrary.Type == "package")
            {
                type = AssetsFileLibraryType.Package;
            }
            else if (lockFileLibrary.Type == "project")
            {
                type = AssetsFileLibraryType.Project;
            }
            else
            {
                library = null;
                return false;
            }

            library = new AssetsFileTargetLibrary(lockFileLibrary, type);
            return true;
        }

        private AssetsFileTargetLibrary(LockFileTargetLibrary library, AssetsFileLibraryType type)
        {
            Name = library.Name;
            Version = library.Version.ToNormalizedString();
            Type = type;

            // TODO use each dependency's version range in caption (won't have parity with top-level item unless we update caption or change SDK to return this information)
            // TODO use each dependency's include/exclude in browse object (won't have parity with top-level item until we rethink browse objects for them)
            Dependencies = library.Dependencies.Select(dep => dep.Id).ToImmutableArray();

            CompileTimeAssemblies = library.CompileTimeAssemblies
                .Select(a => a.Path)
                .Where(path => path != null)
                .Where(path => !NuGetUtils.IsPlaceholderFile(path))
                .ToImmutableArray(); // TODO do we want to use the 'properties' here? maybe for browse object

            FrameworkAssemblies = library.FrameworkAssemblies.ToImmutableArray();

            // TODO filter by code language as well (requires knowing project language): https://github.com/dotnet/NuGet.BuildTasks/blob/5244c490a425353ac12445567d87d674ae118836/src/Microsoft.NuGet.Build.Tasks/ResolveNuGetPackageAssets.cs#L572-L575
            ContentFiles = library.ContentFiles
                .Where(file => !NuGetUtils.IsPlaceholderFile(file.Path))
                .Select(file => new AssetsFileTargetLibraryContentFile(file))
                .ToImmutableArray();
        }

        public string Name { get; }
        public string Version { get; }
        public AssetsFileLibraryType Type { get; }
        public ImmutableArray<string> Dependencies { get; }
        public ImmutableArray<string> FrameworkAssemblies { get; }
        public ImmutableArray<string> CompileTimeAssemblies { get; }
        public ImmutableArray<AssetsFileTargetLibraryContentFile> ContentFiles { get; }

        public override string ToString() => $"{Type} {Name} ({Version}) {Dependencies.Length} {(Dependencies.Length == 1 ? "dependency" : "dependencies")}";
    }
}
