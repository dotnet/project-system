// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Microsoft.VisualStudio.ProjectSystem.VS.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IFileSystem = Microsoft.VisualStudio.IO.IFileSystem;
using Path = Microsoft.IO.Path;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IProjectRetargetHandler))]
[AppliesTo(ProjectCapability.DotNet)]
[Order(Order.Default)]
[method: ImportingConstructor]
internal sealed partial class ProjectRetargetHandler(
#pragma warning disable CS9113 // Only used for scoping to ensure an instance per project
    UnconfiguredProject project,
#pragma warning restore CS9113
    IDotNetReleasesProvider releasesProvider,
    IFileSystem fileSystem,
    IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> projectRetargetingService,
    ISolutionService solutionService,
    IEnvironment environment,
    IDotNetEnvironment dotnetEnvironment)
    : IProjectRetargetHandler, IDisposable
{
    private IVsTrackProjectRetargeting2? _projectRetargeting;
    private Guid _currentSdkDescriptionId = Guid.Empty;
    private Guid _sdkRetargetId = Guid.Empty;

    public async Task<IProjectTargetChange?> CheckForRetargetAsync(RetargetCheckOptions options)
    {
        RetargetCheckOptions flags = RetargetCheckOptions.ProjectRetarget | RetargetCheckOptions.SolutionRetarget | RetargetCheckOptions.ProjectLoad;

        if ((options & flags) == 0)
        {
            return null;
        }

        string? sdkVersion = await GetSdkVersionForSolutionAsync();

        if (sdkVersion is null)
        {
            return null;
        }

        string? retargetVersion = await releasesProvider.GetNewerSupportedSdkVersionAsync(sdkVersion);

        if (retargetVersion is null)
        {
            return null;
        }

        // Check if the retarget is already installed globally
        if (dotnetEnvironment.IsSdkInstalled(retargetVersion))
        {
            return null;
        }

        _projectRetargeting ??= await projectRetargetingService.GetValueAsync();

        if (_currentSdkDescriptionId == Guid.Empty)
        {
            // register the current and retarget versions, note there is a bug in the current implementation
            // ultimately we will need to just set retarget. Right now, we need to register two
            // targets, we want to create two distinct ones, as the bug workaround requires different guids

            var currentSdkDescription = RetargetSDKDescription.Create(retargetVersion, environment.ProcessArchitecture); // this won't be needed
            _projectRetargeting.RegisterProjectTarget(currentSdkDescription);  // this wont be needed.
            _currentSdkDescriptionId = currentSdkDescription.TargetId;
        }

        if (_sdkRetargetId == Guid.Empty)
        {
            var retargetSdkDescription = RetargetSDKDescription.Create(retargetVersion, environment.ProcessArchitecture);
            _projectRetargeting.RegisterProjectTarget(retargetSdkDescription);
            _sdkRetargetId = retargetSdkDescription.TargetId;
        }

        return new TargetChange()
        {
            NewTargetId = _currentSdkDescriptionId, // this is for workaround only
            //NewTargetId = Guid.Empty, // this is what we want ultimately
            CurrentTargetId = _sdkRetargetId
        };

        async Task<string?> GetSdkVersionForSolutionAsync()
        {
            string? solutionDirectory = await solutionService.GetSolutionDirectoryAsync();

            if (!Strings.IsNullOrEmpty(solutionDirectory))
            {
                string? globalJsonPath = FindGlobalJsonPath();

                if (globalJsonPath is not null)
                {
                    try
                    {
                        using Stream stream = fileSystem.OpenTextStream(globalJsonPath);
                        using JsonDocument doc = await JsonDocument.ParseAsync(stream);
                        if (doc.RootElement.TryGetProperty("sdk", out JsonElement sdkProp) &&
                            sdkProp.TryGetProperty("version", out JsonElement versionProp))
                        {
                            return versionProp.GetString();
                        }
                    }
                    catch (Exception ex) when (ex.IsCatchable())
                    {
                    }
                }
            }

            return null;

            string? FindGlobalJsonPath()
            {
                string? dir = solutionDirectory;

                while (!string.IsNullOrEmpty(dir))
                {
                    string globalJsonPath = Path.Join(dir, "global.json");

                    if (fileSystem.FileExists(globalJsonPath))
                    {
                        return globalJsonPath;
                    }

                    dir = Path.GetDirectoryName(dir);
                }

                return null;
            }
        }
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

    public void Dispose()
    {
        if (_currentSdkDescriptionId != Guid.Empty || _sdkRetargetId != Guid.Empty)
        {
            Assumes.NotNull(_projectRetargeting);

            if (_currentSdkDescriptionId != Guid.Empty)
            {
                _projectRetargeting.UnregisterProjectTarget(_currentSdkDescriptionId);
                _currentSdkDescriptionId = Guid.Empty;
            }
            if (_sdkRetargetId != Guid.Empty)
            {
                _projectRetargeting.UnregisterProjectTarget(_sdkRetargetId);
                _sdkRetargetId = Guid.Empty;
            }
        }
    }
}
