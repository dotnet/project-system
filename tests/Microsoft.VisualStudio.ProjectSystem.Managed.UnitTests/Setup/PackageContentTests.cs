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

    [Fact]
    public void NpmPackageFileSizes()
    {
        var actualFileSizes = GetNpmPackageFileSizes();

        // Expected file sizes in bytes. These serve as baselines to catch unexpected size increases or decreases.
        // Update these values when product evolution legitimately changes file sizes.
        var expectedSizes = new Dictionary<string, long>
        {
            // Core assembly
            [@"Microsoft.VisualStudio.ProjectSystem.Managed.dll"] = 2_500_000, // ~2.5MB baseline
            [@"Microsoft.VisualStudio.ProjectSystem.Managed.pdb"] = 1_000_000, // ~1MB baseline for debug symbols

            // Code analysis
            [@"Microsoft.CodeAnalysis.dll"] = 4_000_000, // ~4MB baseline (large Roslyn dependency)

            // Resource assemblies - should be relatively small and consistent
            [@"cs\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000, // ~50KB baseline
            [@"de\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"es\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"fr\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"it\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"ja\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"ko\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"pl\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"pt-BR\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"ru\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"tr\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"zh-Hans\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,
            [@"zh-Hant\Microsoft.VisualStudio.ProjectSystem.Managed.resources.dll"] = 50_000,

            // Hot reload dependencies
            [@"HotReload\net10.0\Microsoft.Extensions.DotNetDeltaApplier.dll"] = 100_000, // ~100KB
            [@"HotReload\net6.0\Microsoft.AspNetCore.Watch.BrowserRefresh.dll"] = 50_000, // ~50KB
            [@"HotReload\net6.0\Microsoft.Extensions.DotNetDeltaApplier.dll"] = 100_000, // ~100KB

            // Package metadata
            [@"exports.json"] = 5_000, // ~5KB
            [@"package.json"] = 2_000, // ~2KB
        };

        ValidateFileSizes(actualFileSizes, expectedSizes, "NPM package");
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

    private static Dictionary<string, long> GetNpmPackageFileSizes()
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
            .ToDictionary(
                filePath => Path.GetRelativePath(packagesDirectory, filePath), 
                filePath => new FileInfo(filePath).Length);
    }

    private static void ValidateFileSizes(Dictionary<string, long> actualSizes, Dictionary<string, long> expectedSizes, string packageName)
    {
        // Tolerance for size variations (20% by default, which is reasonable for compiled code)
        const double tolerancePercentage = 0.20;
        
        var errors = new List<string>();

        foreach (var (fileName, expectedSize) in expectedSizes)
        {
            if (!actualSizes.TryGetValue(fileName, out long actualSize))
            {
                errors.Add($"File '{fileName}' not found in {packageName}");
                continue;
            }

            var tolerance = (long)(expectedSize * tolerancePercentage);
            var minSize = expectedSize - tolerance;
            var maxSize = expectedSize + tolerance;

            if (actualSize < minSize)
            {
                errors.Add($"File '{fileName}' in {packageName} is smaller than expected. Expected: ~{expectedSize:N0} bytes (min: {minSize:N0}), Actual: {actualSize:N0} bytes. This might indicate missing functionality or data.");
            }
            else if (actualSize > maxSize)
            {
                errors.Add($"File '{fileName}' in {packageName} is larger than expected. Expected: ~{expectedSize:N0} bytes (max: {maxSize:N0}), Actual: {actualSize:N0} bytes. This might indicate added code or dependencies that should be reviewed.");
            }
        }

        // Also check for any files in the actual package that aren't in our baseline (but only for key files)
        var unexpectedLargeFiles = actualSizes
            .Where(kvp => !expectedSizes.ContainsKey(kvp.Key) && kvp.Value > 100_000) // Only flag files > 100KB
            .Where(kvp => kvp.Key.EndsWith(".dll") || kvp.Key.EndsWith(".exe") || kvp.Key.EndsWith(".pdb")) // Only key file types
            .ToList();

        foreach (var (fileName, size) in unexpectedLargeFiles)
        {
            errors.Add($"Unexpected large file '{fileName}' in {packageName} ({size:N0} bytes). Consider adding it to the baseline if this is expected.");
        }

        if (errors.Count > 0)
        {
            var errorMessage = $"File size validation failed for {packageName}:\n" + string.Join("\n", errors);
            errorMessage += "\n\nTo fix this:\n";
            errorMessage += "1. If the size changes are expected due to product evolution, update the expected sizes in the test.\n";
            errorMessage += "2. If the size changes are unexpected, investigate what caused the increase/decrease.\n";
            errorMessage += "3. For large increases, consider if new dependencies or code are necessary.\n";
            errorMessage += "4. For large decreases, verify no functionality was accidentally removed.";
            
            throw new Xunit.Sdk.XunitException(errorMessage);
        }
    }
}

#endif

