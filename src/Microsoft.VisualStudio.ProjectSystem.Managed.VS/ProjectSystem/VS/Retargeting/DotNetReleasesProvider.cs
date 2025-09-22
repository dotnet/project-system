// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.Deployment.DotNet.Releases;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using NuGet;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IDotNetReleasesProvider))]
internal class DotNetReleasesProvider : IDotNetReleasesProvider
{
    private const string ReleasesResourceFormat = "Microsoft.VisualStudio...releases.{0}";
    private const string RetargtingAppDataFolder = "ProjectSystem.Retargeting";
    private const string ReleasesFileName = ".releases.json";
    private readonly AsyncLazy<ProductCollection?> _product;
    
    private string? AppDataPath { get; set; }
    private readonly bool _localOnly = false;

    private readonly IVsUIService<SVsShell, IVsShell> _vsShell;
    private readonly IFileSystem _fileSystem;
    private readonly IProjectThreadingService _projectThreadingService;

    private string GetAppDataPath()
    {
        _vsShell.Value.GetProperty((int)__VSSPROPID4.VSSPROPID_LocalAppDataDir, out object pObj);
        if (pObj is string path)
        {
            return path;
        }
        else
        {
            throw new InvalidOperationException("Could not determine local app data path.");
        }
    }

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
                AppDataPath ??= GetAppDataPath();

                string resourcesFileName = Path.Combine(AppDataPath, RetargtingAppDataFolder, ReleasesFileName);

                // force the local file first, then download the latest without awaiting.
                ProductCollection? productCollection = await GetProductCollectionAsync(resourcesFileName, forceLocalFile: true);

                if (!_localOnly)
                {
                    _  = _projectThreadingService.JoinableTaskFactory.RunAsync(async () =>
                    {
                        try
                        {
                            _ = await GetProductCollectionAsync(Path.Combine(AppDataPath, RetargtingAppDataFolder, ReleasesFileName), forceLocalFile: false);
                        }
                        catch
                        {
                        }
                    });
                }

                return productCollection;

            }, _projectThreadingService.JoinableTaskFactory);
    }

    private async Task<ProductCollection?> GetProductCollectionAsync(string resourcesFileName, bool forceLocalFile = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await CreateDefaultFileIfNotExistAsync(resourcesFileName, ReleasesFileName, overwriteIfExists: forceLocalFile);
            return await ProductCollection.GetFromFileAsync(resourcesFileName, downloadLatest: !forceLocalFile);
        }
        catch(Exception)
        {
            // If we fail to load the product collection, return null
            return null;
        }
    }

    private async Task CreateDefaultFileIfNotExistAsync(string path, string resource, bool overwriteIfExists = false)
    {
        if (!_fileSystem.FileExists(path) || overwriteIfExists)
        {
            Assembly assembly = GetType().Assembly;

            var resourceStream = assembly.GetManifestResourceStream(string.Format(ReleasesResourceFormat, resource));

            if (resourceStream is not null)
            {
                string templateEngineHostRoot = Path.GetDirectoryName(path);

                if (!_fileSystem.DirectoryExists(templateEngineHostRoot))
                {
                    _fileSystem.CreateDirectory(templateEngineHostRoot);
                }

                using (resourceStream)
                using (var reader = new StreamReader(resourceStream))
                {
                    string content = await reader.ReadToEndAsync();
                    await _fileSystem.WriteAllTextAsync(path, content);
                }
            }
        }
    }

    private async Task<ProductCollection?> GetProductCollectionAsync(CancellationToken cancellationToken)
    {
        if (AppDataPath is null)
        {
            // this will be initialized by now if it hasnt been already
            AppDataPath = GetAppDataPath();
        }

        return await _product.GetValueAsync(cancellationToken);
    }

    public async Task<string?> GetSupportedOrLatestSdkVersionAsync(
        string? sdkVersion,
        bool includePreview = false,
        CancellationToken cancellationToken = default)
    {
        var products = await GetProductCollectionAsync(cancellationToken);
        if (products is null)
        {
            // could not determine release, just return the same version.
            return sdkVersion;
        }

        if (sdkVersion is not null)
        {
            // Parse the major/minor version from the passed SDK version to find matching product
            if (SemanticVersion.TryParse(sdkVersion, out var parsedSdkVersion))
            {
                // Find the product that matches the major/minor version of the SDK
                var matchingProduct = products.FirstOrDefault(p => 
                {
                    if (SemanticVersion.TryParse(p.ProductVersion, out var productVersion))
                    {
                        return productVersion.Version.Major == parsedSdkVersion.Version.Major && 
                               productVersion.Version.Minor == parsedSdkVersion.Version.Minor;
                    }

                    return false;
                });

                if (matchingProduct is not null && matchingProduct.IsSupported)
                {
                    try
                    {
                        string? supportedSdkVersion = await GetLatestSupportedSdkVersionAsync(sdkVersion, includePreview, matchingProduct);

                        if (supportedSdkVersion is not null)
                        {
                            return supportedSdkVersion;
                        }   
                    }
                    catch (Exception)
                    {
                        // If we fail to get releases, fall back to returning the original version
                        return sdkVersion;
                    }
                }
            }
        }

        // Otherwise, find the highest released SDK version matching the preview/go-live filter and not EOL
        var filtered = products
            .Where(p =>
                p.IsSupported &&
                (includePreview || p.SupportPhase != SupportPhase.Preview) &&
                (includePreview || p.SupportPhase != SupportPhase.GoLive))
            .OrderByDescending(p => SemanticVersion.TryParse(p.ProductVersion, out var v) ? v : new SemanticVersion(new Version(0, 0, 0)));

        var latestProduct = filtered.FirstOrDefault();

        if (latestProduct is not null)
        {
            string? supportedSdkVersion = await GetLatestSupportedSdkVersionAsync(sdkVersion, includePreview, latestProduct);

            if (supportedSdkVersion is not null)
            {
                return supportedSdkVersion;
            }
        }

        return null;

        static async Task<string?> GetLatestSupportedSdkVersionAsync(string? currentVersion, bool includePreview, Product matchingProduct)
        {
            var releases = await matchingProduct.GetReleasesAsync();

            // Find the latest SDK version from the releases that is not preview/go-live
            var latestSdk = releases
                .Where(r => r.Sdks?.Any() is true &&
                           (includePreview || (matchingProduct.SupportPhase != SupportPhase.Preview && matchingProduct.SupportPhase != SupportPhase.GoLive)))
                .SelectMany(r => r.Sdks)
                .OrderByDescending(sdk => sdk.Version)
                .FirstOrDefault();

            if (latestSdk is not null)
            {
                if (currentVersion is not null 
                    && string.Equals(currentVersion, latestSdk.Version.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return currentVersion;
                }

                return latestSdk.DisplayVersion.ToString();
            }

            return null;
        }
    }
}
