// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.Telemetry;

/// <summary>
/// Reports the SDK version via telemetry.
/// </summary>
[Export(ExportContractNames.Scopes.UnconfiguredProject, typeof(IProjectDynamicLoadComponent))]
[AppliesTo(ProjectCapability.DotNet)]
internal class SdkVersionReporter : IProjectDynamicLoadComponent
{
    private readonly IUnconfiguredProjectCommonServices _projectVsServices;
    private readonly ITelemetryService _telemetryService;
    private readonly ISafeProjectGuidService _projectGuidService;
    private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

    [ImportingConstructor]
    public SdkVersionReporter(
        IUnconfiguredProjectCommonServices projectVsServices,
        ISafeProjectGuidService projectGuidService,
        ITelemetryService telemetryService,
        IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
    {
        _projectVsServices = projectVsServices;
        _projectGuidService = projectGuidService;
        _telemetryService = telemetryService;
        _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
    }

    public Task LoadAsync()
    {
        // Do not block initialization on reporting the SDK version. It is possible to deadlock.
        _projectVsServices.ThreadingService.RunAndForget(
            async () =>
            {
                // Wait for the project to be loaded so that we don't prematurely load the active configuration.
                await _unconfiguredProjectTasksService.ProjectLoadedInHost;

                (string? version, Guid projectGuid) = await(
                    GetSdkVersionAsync(),
                    _projectGuidService.GetProjectGuidAsync());

                if (!Strings.IsNullOrEmpty(version) && projectGuid != Guid.Empty)
                {
                    _telemetryService.PostProperties(
                        TelemetryEventName.SDKVersion,
                        new[]
                        {
                            (TelemetryPropertyName.SDKVersion.Project, (object)projectGuid.ToString()),
                            (TelemetryPropertyName.SDKVersion.NETCoreSDKVersion, version)
                        });
                }
            },
            unconfiguredProject: _projectVsServices.Project);

        return Task.CompletedTask;
    }

    protected virtual async Task<string?> GetSdkVersionAsync()
    {
        // This method is virtual to support mocking for unit testing.

        // We use TryGetCurrentConfigurationGeneralPropertiesSnapshot rather than GetConfigurationGeneralPropertiesAsync
        // as the latter will take a project read lock, while the former uses the most recent available
        // snapshot. For the SDK version, this is unlikely to change throughout the lifetime of the project,
        // so even an older version of this value will be fine.
        if (!_projectVsServices.ActiveConfiguredProjectProperties.TryGetCurrentConfigurationGeneralPropertiesSnapshot(out ConfigurationGeneral projectProperties, requiredToMatchProjectVersion: false))
        {
            // Given we waited for the project to load above, this should never happen.
            System.Diagnostics.Debug.Fail("Unable to get ConfigurationGeneral properties snapshot for SDK version.");
            return null;
        }

        Task<object?>? task = projectProperties?.NETCoreSdkVersion?.GetValueAsync();
        string? version = task is null ? null : (string?)await task;
        return version;
    }

    public Task UnloadAsync()
    {
        return Task.CompletedTask;
    }
}
