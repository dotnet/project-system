// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.Deployment.DotNet.Releases;
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
                        catch (Exception)
                        {
                            // TODO: Log
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
            // Try to find the requested SDK version
            var requested = products.FirstOrDefault(p => p.ProductVersion.Equals(sdkVersion, StringComparison.OrdinalIgnoreCase));
            if (requested is not null && !requested.IsSupported)
            {
                return requested.ProductVersion;
            }
        }

        // Otherwise, find the highest released SDK version matching the preview/go-live filter and not EOL
        var filtered = products
            .Where(p =>
                p.IsSupported &&
                (includePreview || p.SupportPhase != SupportPhase.Preview) &&
                (includePreview || p.SupportPhase != SupportPhase.GoLive))
            .OrderByDescending(p => SemanticVersion.TryParse(p.ProductVersion, out var v) ? v : new SemanticVersion(new Version(0, 0, 0)));

        return filtered.FirstOrDefault()?.ProductVersion;
    }
}
