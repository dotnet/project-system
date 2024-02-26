// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

// The NPM package is only relevant to the net8.0 build.
#if NETCOREAPP

using Microsoft.VisualStudio.Utilities;
using VerifyXunit;

namespace Microsoft.VisualStudio.Setup
{
    [UsesVerify]
    public sealed class PackageContentTests
    {
        [Fact]
        public Task NpmPackage()
        {
            IEnumerable<string> files = GetNpmPackageContents();
            return Verifier.Verify(files);
        }

        private static IEnumerable<string> GetNpmPackageContents()
        {
            var rootPath = RepoUtil.FindRepoRootPath();

#if DEBUG
            var config = "Debug";
#elif RELEASE
            var config = "Release";
#else
#error Unexpected configuration
#endif

            var packagesDirectory = Path.Combine(
                rootPath,
                "artifacts",
                config,
                "obj",
                "Microsoft.VisualStudio.ProjectSystem.Managed",
                "net8.0",
                "npmsrc");

            return Directory.EnumerateFiles(packagesDirectory, "*", SearchOption.AllDirectories)
                .Select(pullPath => Path.GetRelativePath(packagesDirectory, pullPath));
        }
    }
}

#endif

