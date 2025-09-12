// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Text.Json;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargeting;

[Export(typeof(IProjectRetargetHandler))]
[AppliesTo(ProjectCapability.DotNet)]
[Order(Order.Default)]
internal sealed partial class ProjectRetargetHandler : IProjectRetargetHandler
{
    private readonly UnconfiguredProject _unconfiguredProject;
    private readonly IDotNetReleasesProvider _releasesProvider;
    private readonly IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> _projectRetargetingService; 

    [ImportingConstructor]
    public ProjectRetargetHandler(
        UnconfiguredProject unconfiguredProject,
        IDotNetReleasesProvider releasesProvider,
        IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> projectRetargetingService)
    {
        _unconfiguredProject = unconfiguredProject;
        _releasesProvider = releasesProvider;
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
        // this is not called by the current retargeting flow, so we can return an empty list.
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

        // see sample impl:
        // https://devdiv.visualstudio.com/DevDiv/_git/VS?path=/src/env/vscore/package/Solutions/ProjectSystems/StubHierarchy.cs&version=GBdev/chzuluag/newIPARefactor&_a=contents

        // prototype implementation - in the future we will determine the actual target change
        //return GetPrototypeTargetChange(retargetingService);

        // real implementation - determine if retarget is needed, and if so, register the targets
        // after we don't need the prototype or workaround code, refactor this.
        // it is factored like this to be able to switch between the two implementations easily
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

        // register the current and retarget versions, note there is a bug in the current implementation
        // ultimately we will need to just set retarget. Right now, we need to register two
        // targets, we want to create two distinct ones, as the bug workaround requires different guids
        var currentSDKDescription = RetargetSDKDescription.Create(retargetVersion); // this won't be needed
        var retargetDescription = RetargetSDKDescription.Create(retargetVersion);

        retargetingService.RegisterProjectTarget(currentSDKDescription);  // this wont be needed.
        retargetingService.RegisterProjectTarget(retargetDescription);

        return new TargetChange()
        {
            NewTargetId = currentSDKDescription.TargetId, // this is for workaround only
            //NewTargetId = Guid.Empty, // this is what we want ultimately
            CurrentTargetId = retargetDescription.TargetId
        };
    }
        
    //private static TargetChange GetPrototypeTargetChange(IVsTrackProjectRetargeting2 retargetingService)
    //{

    //    var currentSDKDescription = RetargetSDKDescription.Create("8.0.402");
    //    var retargetDescription = RetargetSDKDescription.Create("8.0.402");

    //    retargetingService.RegisterProjectTarget(currentSDKDescription);
    //    retargetingService.RegisterProjectTarget(retargetDescription);

    //    // consistent with current sample above, we set the current to the retarget, and new to empty
    //    // since we arent really doing a retarget, just providing guidance
    //    return new TargetChange()
    //    {
    //        NewTargetId = currentSDKDescription.TargetId,
    //        //NewTargetId = Guid.Empty,
    //        CurrentTargetId = retargetDescription.TargetId
    //    };
    //}

    private async Task<string?> GetSdkVersionForProjectAsync()
    {
        // TODO: implement a more robust way to get the SDK version for the project

        // 1. Look for global.json in project or parent directories
        var projectPath = _unconfiguredProject.FullPath;
        var dir = Path.GetDirectoryName(projectPath);
        while (!string.IsNullOrEmpty(dir))
        {
            var globalJsonPath = Path.Combine(dir, "global.json");
            if (File.Exists(globalJsonPath))
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
                catch { /* ignore parse errors */ }
                break;
            }
            dir = Path.GetDirectoryName(dir);
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
}
