// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Text.Json;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IProjectRetargetHandler))]
[AppliesTo(ProjectCapability.DotNet)]
[Order(Order.Default)]
internal sealed partial class ProjectRetargetHandler : IProjectRetargetHandler, IDisposable
{
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly IDotNetReleasesProvider _releasesProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IProjectThreadingService _projectThreadingService;
    private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _projectRetargetingService;

    private Guid _currentSdkDescriptionId = Guid.Empty;
    private Guid _sdkRetargetId = Guid.Empty;

    [ImportingConstructor]
    public ProjectRetargetHandler(
        UnconfiguredProject unconfiguredProject,
        IDotNetReleasesProvider releasesProvider,
        IFileSystem fileSystem,
        IProjectThreadingService projectThreadingService,
        IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> projectRetargetingService)
    {
        _unconfiguredProject = unconfiguredProject;
        _releasesProvider = releasesProvider;
        _fileSystem = fileSystem;
        _projectThreadingService = projectThreadingService;
        _projectRetargetingService = projectRetargetingService;
    }

    public async Task<IProjectTargetChange?> CheckForRetargetAsync(RetargetCheckOptions options)
    {
        if (!options.HasFlag(RetargetCheckOptions.ProjectRetarget) && !options.HasFlag(RetargetCheckOptions.SolutionRetarget))
        {
            return null;
        }

        return await GetTargetChangeAndRegisterTargetsAsync();
    }

    public Task<IImmutableList<string>> GetAffectedFilesAsync(IProjectTargetChange projectTargetChange)
    {
        // empty file list, as we are only providing guidance.
        return Task.FromResult<IImmutableList<string>>(ImmutableList<string>.Empty);
    }

    public Task RetargetAsync(TextWriter outputLogger, RetargetOptions options, IProjectTargetChange projectTargetChange, string backupLocation)
    {
        // no operation needed, as we are only providing guidance.
        return Task.CompletedTask;
    }

    private async Task<IProjectTargetChange?> GetTargetChangeAndRegisterTargetsAsync()
    {
        var retargetingService = await _projectRetargetingService.GetValueOrNullAsync();

        if (retargetingService is null)
        {
            return null;
        }

        return await GetTargetChangeAsync(retargetingService);
    }

    private async Task<TargetChange?> GetTargetChangeAsync(IVsTrackProjectRetargeting2 retargetingService)
    {
        var sdkVersion = await GetSdkVersionForProjectAsync();

        var retargetVersion = await _releasesProvider.GetSupportedOrLatestSdkVersionAsync(sdkVersion, includePreview: true);

        if (retargetVersion is null || string.Equals(sdkVersion, retargetVersion, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (_currentSdkDescriptionId == Guid.Empty)
        {
            // register the current and retarget versions, note there is a bug in the current implementation
            // ultimately we will need to just set retarget. Right now, we need to register two
            // targets, we want to create two distinct ones, as the bug workaround requires different guids

            var currentSDKDescription = RetargetSDKDescription.Create(retargetVersion); // this won't be needed
            retargetingService.RegisterProjectTarget(currentSDKDescription);  // this wont be needed.
            _currentSdkDescriptionId = currentSDKDescription.TargetId;
        }

        if (_sdkRetargetId == Guid.Empty)
        {
            var retargetSDKDescription = RetargetSDKDescription.Create(retargetVersion); // this won't be needed
            retargetingService.RegisterProjectTarget(retargetSDKDescription);  // this wont be needed.
            _sdkRetargetId = retargetSDKDescription.TargetId;
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
        var dir = startingDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var globalJsonPath = Path.Combine(dir, "global.json");
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
        // 1. Look for global.json in project or parent directories
        var projectPath = _unconfiguredProject.FullPath;
        var projectDirectory = Path.GetDirectoryName(projectPath);
        
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            string? globalJsonPath = FindGlobalJsonPath(projectDirectory);
            if (globalJsonPath is not null)
            {
                try
                {
                    using var stream = File.OpenRead(globalJsonPath);
                    using var doc = await JsonDocument.ParseAsync(stream);
                    if (doc.RootElement.TryGetProperty("sdk", out var sdkProp) &&
                        sdkProp.TryGetProperty("version", out var versionProp))
                    {
                        return versionProp.GetString();
                    }
                }
                catch /* ignore errors */
                {
                }
            }
        }

        // 2. Fallback: use `dotnet --version` to get the default SDK
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();
            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_currentSdkDescriptionId != Guid.Empty || _sdkRetargetId != Guid.Empty)
        {
            _projectThreadingService.JoinableTaskFactory.Run(async () =>
            {
                var retargetingService = await _projectRetargetingService.GetValueOrNullAsync();
                if (retargetingService is not null)
                {
                    if (_currentSdkDescriptionId != Guid.Empty)
                    {
                        retargetingService.UnregisterProjectTarget(_currentSdkDescriptionId);
                        _currentSdkDescriptionId = Guid.Empty;
                    }
                    if (_sdkRetargetId != Guid.Empty)
                    {
                        retargetingService.UnregisterProjectTarget(_sdkRetargetId);
                        _sdkRetargetId = Guid.Empty;
                    }
                }
            });
        }
    }
}
