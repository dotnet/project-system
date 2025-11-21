// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Deployment.DotNet.Releases;
using Microsoft.VisualStudio.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Path = Microsoft.IO.Path;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IDotNetReleasesProvider))]
internal class DotNetReleasesProvider : IDotNetReleasesProvider
{
    private const string RetargetingAppDataFolder = @"ProjectSystem\Retargeting";
    private const string ReleasesFileName = ".releases.json";

    private readonly AsyncLazy<ProductCollection?> _productCollection;
    private readonly AsyncLazy<string> _appDataPath;
    private readonly IVsUIService<IVsShell> _vsShell;
    private readonly IProjectThreadingService _projectThreadingService;

    private ImmutableDictionary<string, AsyncLazy<IReadOnlyCollection<ProductRelease>>> _productReleasesByProductVersion = ImmutableStringDictionary<AsyncLazy<IReadOnlyCollection<ProductRelease>>>.EmptyOrdinal;

    [ImportingConstructor]
    public DotNetReleasesProvider(
        IVsUIService<SVsShell, IVsShell> vsShell,
        IProjectThreadingService projectThreadingService)
    {
        _vsShell = vsShell;
        _projectThreadingService = projectThreadingService;

        _appDataPath = new AsyncLazy<string>(
            async () =>
            {
                await _projectThreadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

                HResult.Verify(
                    _vsShell.Value.GetProperty((int)__VSSPROPID4.VSSPROPID_LocalAppDataDir, out object pObj),
                    $"Error getting local appdata dir in {typeof(DotNetReleasesProvider)}.");

                if (pObj is string path)
                {
                    // e.g. "%LocalAppData%\Microsoft\VisualStudio\18.0_721820d7\"
                    return path;
                }
                else
                {
                    throw new InvalidOperationException("Could not determine local app data path.");
                }
            },
            _projectThreadingService.JoinableTaskFactory);

        _productCollection = new AsyncLazy<ProductCollection?>(
            async () =>
            {
                string appDataPath = await _appDataPath.GetValueAsync();

                string resourcesFileName = Path.Join(appDataPath, RetargetingAppDataFolder, ReleasesFileName);

                // NOTE this is doing network and disk IO on the main thread, but the retargeting APIs are
                // called on the main thread so it's not clear what we can do about this.

                try
                {
                    return await ProductCollection.GetFromFileAsync(resourcesFileName, downloadLatest: true);
                }
                catch
                {
                    // If we fail to load the product collection, return null
                    return null;
                }
            },
            _projectThreadingService.JoinableTaskFactory);
    }

    public async Task<string?> GetNewerSupportedSdkVersionAsync(string sdkVersion, CancellationToken cancellationToken = default)
    {
        ProductCollection? products = await _productCollection.GetValueAsync(cancellationToken);

        if (products is null)
        {
            // could not determine release, just return the same version.
            return null;
        }

        if (ReleaseVersion.TryParse(sdkVersion, out ReleaseVersion? parsedSdkVersion))
        {
            // Find the product that matches the major/minor version of the SDK
            Product? matchingProduct = products.FirstOrDefault(
                p => p.LatestSdkVersion.Major == parsedSdkVersion.Major &&
                     p.LatestSdkVersion.Minor == parsedSdkVersion.Minor &&
                     p.LatestSdkVersion.IsLaterThan(parsedSdkVersion));

            if (matchingProduct is not null)
            {
                try
                {
                    return await GetLatestSupportedSdkVersionAsync(parsedSdkVersion, matchingProduct);
                }
                catch
                {
                    // we can just fall through and return null here
                }
            }
        }

        return null;

        async Task<string?> GetLatestSupportedSdkVersionAsync(ReleaseVersion currentVersion, Product matchingProduct)
        {
            if (matchingProduct.SupportPhase is SupportPhase.Active or SupportPhase.Maintenance or SupportPhase.EOL)
            {
                // For these support phases, we can use the SDK version defined directly on the product
                // and avoid downloading the lengthy release data for that particular version.
                return matchingProduct.LatestSdkVersion.ToString();
            }

            // TODO in future we want EOL phase to recommend the user move to the highest supported active SDK version.
            // Should this suggest only LTS or also STS?

            AsyncLazy<IReadOnlyCollection<ProductRelease>> lazy = ImmutableInterlocked.GetOrAdd(
                ref _productReleasesByProductVersion,
                key: matchingProduct.ProductVersion,
                valueFactory: (key, arg) => new AsyncLazy<IReadOnlyCollection<ProductRelease>>(
                    async () =>
                    {
                        string appDataPath = await _appDataPath.GetValueAsync();

                        string resourceFileName = Path.Combine(appDataPath, RetargetingAppDataFolder, $"{key}{ReleasesFileName}");

                        return await GetReleasesAsync(arg, resourceFileName, key) ?? [];
                    },
                    _projectThreadingService.JoinableTaskFactory),
                factoryArgument: matchingProduct);

            IReadOnlyCollection<ProductRelease> releases = await lazy.GetValueAsync(cancellationToken);

            // Find the latest SDK version.
            SdkReleaseComponent? latestSdk = releases
                .SelectMany(r => r.Sdks)
                .MaxByOrDefault(sdk => sdk.Version);

            if (latestSdk is not null)
            {
                if (currentVersion?.Equals(latestSdk.Version) is true)
                {
                    return currentVersion.ToString();
                }

                return latestSdk.DisplayVersion.ToString();
            }

            return null;
        }

        async Task<IReadOnlyCollection<ProductRelease>?> GetReleasesAsync(Product product, string resourceFileName, string version, CancellationToken cancellationToken = default)
        {
            try
            {
                return await product.GetReleasesAsync(resourceFileName, downloadLatest: true);
            }
            catch
            {
                // if we fail to load the releases, return null
                return null;
            }
        }
    }
}
