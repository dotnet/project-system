// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.Deployment.DotNet.Releases;
using Microsoft.VisualStudio.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.Shell.Interop;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IDotNetReleasesProvider))]
internal class DotNetReleasesProvider : IDotNetReleasesProvider
{
    private const string RetargetingAppDataFolder = "ProjectSystem.Retargeting";
    private const string ReleasesFileName = ".releases.json";
    private readonly AsyncLazy<ProductCollection?> _product;

    private string AppDataPath => field ??= GetAppDataPath();
    private readonly bool _localOnly = false;

    private readonly IVsUIService<SVsShell, IVsShell> _vsShell;
    private readonly IFileSystem _fileSystem;
    private readonly IProjectThreadingService _projectThreadingService;

    private readonly ConcurrentDictionary<string, JoinableTask> _releaseUpdateTasksByProductVersion = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IReadOnlyCollection<ProductRelease>> _productReleasesByProductVersion = new(StringComparer.OrdinalIgnoreCase);

    [ImportingConstructor]
    public DotNetReleasesProvider(
        IVsUIService<SVsShell, IVsShell> vsShell,
        IFileSystem fileSystem,
        IProjectThreadingService projectThreadingService)
    {
        _vsShell = vsShell;
        _projectThreadingService = projectThreadingService;
        _fileSystem = fileSystem;
        _product = new AsyncLazy<ProductCollection?>(
            async () =>
            {
                string resourcesFileName = Path.Combine(AppDataPath, RetargetingAppDataFolder, ReleasesFileName);

                // force the local file first, then download the latest without awaiting.
                ProductCollection? productCollection = await GetProductCollectionAsync(resourcesFileName, forceLocalFile: _localOnly);

                if (!_localOnly)
                {
                    _ = _projectThreadingService.JoinableTaskFactory.RunAsync(async () =>
                    {
                        try
                        {
                            _ = await GetProductCollectionAsync(resourcesFileName, forceLocalFile: false);
                        }
                        catch
                        {
                        }
                    });
                }

                return productCollection;

            }, _projectThreadingService.JoinableTaskFactory);
    }

    private string GetAppDataPath()
    {
        HResult.Verify(
            _vsShell.Value.GetProperty((int)__VSSPROPID4.VSSPROPID_LocalAppDataDir, out object pObj),
            $"Error getting local appdata dir in {typeof(DotNetReleasesProvider)}.");

        if (pObj is string path)
        {
            return path;
        }
        else
        {
            throw new InvalidOperationException("Could not determine local app data path.");
        }
    }

    private async Task<ProductCollection?> GetProductCollectionAsync(string resourcesFileName, bool forceLocalFile = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await CreateDefaultFileIfNotExistAsync(resourcesFileName, ReleasesFileName, overwriteIfExists: false);
            return await ProductCollection.GetFromFileAsync(resourcesFileName, downloadLatest: !forceLocalFile);
        }
        catch (Exception)
        {
            // If we fail to load the product collection, return null
            return null;
        }
    }

    private Task CreateDefaultFileIfNotExistAsync(string path, string resource, bool overwriteIfExists = false)
    {
        if (!_fileSystem.FileExists(path) || overwriteIfExists)
        {
            Assembly assembly = GetType().Assembly;
            string cachedFile = Path.Combine(Path.GetDirectoryName(assembly.Location), ".releases", resource);

            if (_fileSystem.FileExists(cachedFile))
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));
                _fileSystem.CopyFile(cachedFile, path, overwriteIfExists);
            }
        }

        return Task.CompletedTask;
    }

    private async Task<ProductCollection?> GetProductCollectionAsync(CancellationToken cancellationToken)
    {
        return await _product.GetValueAsync(cancellationToken);
    }

    public async Task<ReleaseVersion?> GetSupportedOrLatestSdkVersionAsync(
        ReleaseVersion? sdkVersion,
        bool includePreview = false,
        CancellationToken cancellationToken = default)
    {
        ProductCollection? products = await GetProductCollectionAsync(cancellationToken);
        if (products is null)
        {
            // could not determine release, just return the same version.
            return sdkVersion;
        }

        if (sdkVersion is not null)
        {
            // Find the product that matches the major/minor version of the SDK
            Product matchingProduct = products.FirstOrDefault( p => p.LatestSdkVersion.Major == sdkVersion.Major &&
                p.LatestSdkVersion.Minor == sdkVersion.Minor && p.LatestSdkVersion.IsLaterThan(sdkVersion));

            if (matchingProduct is not null)
            {
                try
                {
                    return await GetLatestSupportedSdkVersionAsync(sdkVersion, includePreview, matchingProduct);
                }
                catch
                {
                    // we can just fall through and return null here
                }
            }
        }

        return null;
    }

    private async Task<ReleaseVersion?> GetLatestSupportedSdkVersionAsync(ReleaseVersion? currentVersion, bool includePreview, Product matchingProduct)
    {
        string resourceFileName = Path.Combine(AppDataPath, RetargetingAppDataFolder, $"{matchingProduct.ProductVersion}{ReleasesFileName}");

        JoinableTask? updateTask = null;

        if (!_productReleasesByProductVersion.TryGetValue(matchingProduct.ProductVersion, out IReadOnlyCollection<ProductRelease>? releases))
        {
            // grab already downloaded release, then kick off a background task to update the releases cache
            if (_releaseUpdateTasksByProductVersion.TryGetValue(matchingProduct.ProductVersion, out updateTask))
            {
                // If there's an existing update task for this product version, wait for it to complete
                await updateTask;
            }

            releases = await GetReleasesAsync(matchingProduct, resourceFileName, matchingProduct.ProductVersion, forceLocalFile: _localOnly);
            _productReleasesByProductVersion[matchingProduct.ProductVersion] = releases ?? [];
        }

        if (!_localOnly && updateTask is null)
        {
            // kick off a background task to update the releases cache, but don't await it.
            // it will be awaited on the next call if it hasn't completed yet.
            _ = _releaseUpdateTasksByProductVersion.GetOrAdd(matchingProduct.ProductVersion,
                _projectThreadingService.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        _ = await GetReleasesAsync(matchingProduct, resourceFileName, matchingProduct.ProductVersion, forceLocalFile: false);
                    }
                    catch
                    {
                    }
                }));
        }

        // Find the latest SDK version from the releases that is not preview/go-live
        SdkReleaseComponent? latestSdk = releases
            .Where(r => r.Sdks?.Any() is true &&
                       (includePreview || (matchingProduct.SupportPhase != SupportPhase.Preview && matchingProduct.SupportPhase != SupportPhase.GoLive)))
            .SelectMany(r => r.Sdks)
            .MaxByOrDefault(sdk => sdk.Version);

        if (latestSdk is not null)
        {
            if (currentVersion is not null
                && string.Equals(currentVersion.ToString(), latestSdk.Version.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return currentVersion;
            }

            return ReleaseVersion.Parse(latestSdk.DisplayVersion.ToString());
        }

        return null;
    }
    
    private async Task<IReadOnlyCollection<ProductRelease>?> GetReleasesAsync(Product product, string resourceFileName, string version, bool forceLocalFile = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await CreateDefaultFileIfNotExistAsync(resourceFileName, $"{version}{ReleasesFileName}", overwriteIfExists: forceLocalFile);
            return await product.GetReleasesAsync(resourceFileName, downloadLatest: !forceLocalFile);
        }
        catch
        {
            // if we fail to load the releases, return null
            return null;
        }
    }
}

