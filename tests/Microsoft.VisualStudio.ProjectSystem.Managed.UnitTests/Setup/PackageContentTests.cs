// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

// The NPM package is only relevant to the net8.0 build.
#if NETCOREAPP

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Setup;

public sealed class PackageContentTests
{
    [Fact]
    public void NpmPackage()
    {
        var actual = GetNpmPackageContents();
        var expected = new[]
        {
            @"cs\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"de\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"es\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"exports.json",
            @"fr\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"HotReload\net10.0\Microsoft.Extensions.DotNetDeltaApplier.dll",
            @"HotReload\net6.0\Microsoft.AspNetCore.Watch.BrowserRefresh.dll",
            @"HotReload\net6.0\Microsoft.Extensions.DotNetDeltaApplier.dll",
            @"it\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"ja\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"ko\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"Microsoft.CodeAnalysis.dll",
            @"Microsoft.VisualStudio.ProjectSystem.Managed.dll",
            @"Microsoft.VisualStudio.ProjectSystem.Managed.pdb",
            @"package.json",
            @"pl\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"pt-BR\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"ru\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"tr\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"zh-Hans\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
            @"zh-Hant\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll",
        };

        AssertEx.SequenceEqual(expected, actual);
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
            "net9.0",
            "npmsrc");

        return Directory.EnumerateFiles(packagesDirectory, "*", SearchOption.AllDirectories)
            .Select(pullPath => Path.GetRelativePath(packagesDirectory, pullPath))
            .OrderBy(path => path);
    }
}

#endif

