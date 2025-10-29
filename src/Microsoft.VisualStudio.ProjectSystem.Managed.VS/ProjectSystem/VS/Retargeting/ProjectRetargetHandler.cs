// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Microsoft.VisualStudio.ProjectSystem.VS.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IProjectRetargetHandler))]
[AppliesTo(ProjectCapability.DotNet)]
[Order(Order.Default)]
internal sealed partial class ProjectRetargetHandler : IProjectRetargetHandler, IDisposable
{
    private readonly Lazy<IDotNetReleasesProvider> _releasesProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IProjectThreadingService _projectThreadingService;
    private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _projectRetargetingService;
    private readonly IVsService<SVsSolution, IVsSolution> _solutionService;
    private readonly IEnvironment _environment;
    private readonly IDotNetEnvironment _dotnetEnvironment;

    private Guid _currentSdkDescriptionId = Guid.Empty;
    private Guid _sdkRetargetId = Guid.Empty;

    [ImportingConstructor]
    public ProjectRetargetHandler(
        Lazy<IDotNetReleasesProvider> releasesProvider,
        IFileSystem fileSystem,
        IProjectThreadingService projectThreadingService,
        IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> projectRetargetingService,
        IVsService<SVsSolution, IVsSolution> solutionService,
        IEnvironment environment,
        IDotNetEnvironment dotnetEnvironment)
    {
        _releasesProvider = releasesProvider;
        _fileSystem = fileSystem;
        _projectThreadingService = projectThreadingService;
        _projectRetargetingService = projectRetargetingService;
        _solutionService = solutionService;
        _environment = environment;
        _dotnetEnvironment = dotnetEnvironment;
    }

    public Task<IProjectTargetChange?> CheckForRetargetAsync(RetargetCheckOptions options)
    {
        if ((options & RetargetCheckOptions.ProjectRetarget) == 0
            && (options & RetargetCheckOptions.SolutionRetarget) == 0
            && (options & RetargetCheckOptions.ProjectLoad) == 0)
        {
            return TaskResult.Null<IProjectTargetChange>();
        }

        return GetTargetChangeAndRegisterTargetsAsync();
    }

    public Task<IImmutableList<string>> GetAffectedFilesAsync(IProjectTargetChange projectTargetChange)
    {
        // empty file list, as we are only providing guidance.
        return TaskResult.EmptyImmutableList<string>();
    }

    public Task RetargetAsync(TextWriter outputLogger, RetargetOptions options, IProjectTargetChange projectTargetChange, string backupLocation)
    {
        // no operation needed, as we are only providing guidance.
        return Task.CompletedTask;
    }

    private async Task<IProjectTargetChange?> GetTargetChangeAndRegisterTargetsAsync()
    {
        IVsTrackProjectRetargeting2? retargetingService = await _projectRetargetingService.GetValueOrNullAsync();

        if (retargetingService is null)
        {
            return null;
        }

        return await GetTargetChangeAsync(retargetingService);
    }

    private async Task<TargetChange?> GetTargetChangeAsync(IVsTrackProjectRetargeting2 retargetingService)
    {
        string? sdkVersion = await GetSdkVersionForProjectAsync();

        if (sdkVersion is null)
        {
            return null;
        }

        string? retargetVersion = await _releasesProvider.Value.GetSupportedOrLatestSdkVersionAsync(sdkVersion, includePreview: true);

        if (retargetVersion is null || sdkVersion == retargetVersion)
        {
            return null;
        }

        // Check if the retarget is already installed globally
        if (_dotnetEnvironment.IsSdkInstalled(retargetVersion))
        {
            return null;
        }

        string architecture = _environment.ProcessArchitecture.ToString().ToLowerInvariant();

        if (_currentSdkDescriptionId == Guid.Empty)
        {
            // register the current and retarget versions, note there is a bug in the current implementation
            // ultimately we will need to just set retarget. Right now, we need to register two
            // targets, we want to create two distinct ones, as the bug workaround requires different guids

            IVsProjectTargetDescription currentSdkDescription = RetargetSDKDescription.Create(retargetVersion.ToString(), architecture); // this won't be needed
            retargetingService.RegisterProjectTarget(currentSdkDescription);  // this wont be needed.
            _currentSdkDescriptionId = currentSdkDescription.TargetId;
        }

        if (_sdkRetargetId == Guid.Empty)
        {
            IVsProjectTargetDescription retargetSdkDescription = RetargetSDKDescription.Create(retargetVersion.ToString(), architecture);
            retargetingService.RegisterProjectTarget(retargetSdkDescription);
            _sdkRetargetId = retargetSdkDescription.TargetId;
        }

        return new TargetChange()
        {
            NewTargetId = _currentSdkDescriptionId, // this is for workaround only
            //NewTargetId = Guid.Empty, // this is what we want ultimately
            CurrentTargetId = _sdkRetargetId
        };
    }

    private string? FindGlobalJsonPath(string startingDirectory)
    {
        string dir = startingDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string globalJsonPath = Path.Combine(dir, "global.json");
            if (_fileSystem.FileExists(globalJsonPath))
            {
                return globalJsonPath;
            }
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    private async Task<string?> GetSdkVersionForProjectAsync()
    {
        string? solutionDirectory = await GetSolutionDirectoryAsync();

        if (!string.IsNullOrEmpty(solutionDirectory))
        {
            string? globalJsonPath = FindGlobalJsonPath(solutionDirectory!);
            if (globalJsonPath is not null)
            {
                try
                {
                    using Stream stream = _fileSystem.OpenTextStream(globalJsonPath);
                    using JsonDocument doc = await JsonDocument.ParseAsync(stream);
                    if (doc.RootElement.TryGetProperty("sdk", out JsonElement sdkProp) &&
                        sdkProp.TryGetProperty("version", out JsonElement versionProp))
                    {
                        return versionProp.GetString();
                    }
                }
                catch /* ignore errors */
                {
                }
            }
        }

        return null;
    }

    private async Task<string?> GetSolutionDirectoryAsync()
    {
        IVsSolution? solution = await _solutionService.GetValueOrNullAsync();

        if (solution is not null)
        {
            int hr = solution.GetSolutionInfo(out string solutionDirectory, out string _, out string _);
            if (hr == HResult.OK && !string.IsNullOrEmpty(solutionDirectory))
            {
                return solutionDirectory;
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_currentSdkDescriptionId != Guid.Empty || _sdkRetargetId != Guid.Empty)
        {
            _projectThreadingService.JoinableTaskFactory.Run(async () =>
            {
                IVsTrackProjectRetargeting2? retargetingService = await _projectRetargetingService.GetValueOrNullAsync();
                if (_currentSdkDescriptionId != Guid.Empty)
                {
                    retargetingService?.UnregisterProjectTarget(_currentSdkDescriptionId);
                    _currentSdkDescriptionId = Guid.Empty;
                }
                if (_sdkRetargetId != Guid.Empty)
                {
                    retargetingService?.UnregisterProjectTarget(_sdkRetargetId);
                    _sdkRetargetId = Guid.Empty;
                }
            });
        }
    }
}
