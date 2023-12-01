// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.Telemetry
{
    internal partial class SDKVersionTelemetryServiceComponent
    {
        internal class SDKVersionTelemetryServiceInstance : OnceInitializedOnceDisposed, IMultiLifetimeInstance
        {
            private readonly IUnconfiguredProjectCommonServices _projectVsServices;
            private readonly ISafeProjectGuidService _projectGuidService;
            private readonly ITelemetryService _telemetryService;
            private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;

            [ImportingConstructor]
            public SDKVersionTelemetryServiceInstance(
                IUnconfiguredProjectCommonServices projectVsServices,
                ISafeProjectGuidService projectGuidService,
                ITelemetryService telemetryService,
                IUnconfiguredProjectTasksService unconfiguredProjectTasksService)
                : base(synchronousDisposal: true)
            {
                _projectVsServices = projectVsServices;
                _projectGuidService = projectGuidService;
                _telemetryService = telemetryService;
                _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
            }

            protected override void Initialize()
            {
                // Do not block initialization on reporting the SDK version. It is possible to deadlock.
                _projectVsServices.ThreadingService.RunAndForget(async () =>
                {
                    // Wait for the project to be loaded so that we don't prematurely load the active configuration.
                    await _unconfiguredProjectTasksService.ProjectLoadedInHost;

                    // We use TryGetCurrentConfigurationGeneralPropertiesSnapshot rather than GetConfigurationGeneralPropertiesAsync
                    // as the latter will take a project read lock, while the former uses the most recent available
                    // snapshot. For the SDK version, this is unlikely to change throughout the lifetime of the project,
                    // so even an older version of this value will be fine.
                    if (!_projectVsServices.ActiveConfiguredProjectProperties.TryGetCurrentConfigurationGeneralPropertiesSnapshot(out ConfigurationGeneral projectProperties, requiredToMatchProjectVersion: false))
                    {
                        // Given we waited for the project to load above, this should never happen.
                        System.Diagnostics.Debug.Fail("Unable to get ConfigurationGeneral properties snapshot for SDK version.");
                        return;
                    }

                    Task<object?>? task = projectProperties?.NETCoreSdkVersion?.GetValueAsync();
                    string? version = task is null ? string.Empty : (string?)await task;
                    Guid projectGuid = await _projectGuidService.GetProjectGuidAsync();

                    if (Strings.IsNullOrEmpty(version) || projectGuid == Guid.Empty)
                    {
                        return;
                    }

                    _telemetryService.PostProperties(TelemetryEventName.SDKVersion, new[]
                    {
                        (TelemetryPropertyName.SDKVersion.Project, (object)projectGuid.ToString()),
                        (TelemetryPropertyName.SDKVersion.NETCoreSDKVersion, version)
                    });
                },
                unconfiguredProject: _projectVsServices.Project);
            }

            protected override void Dispose(bool disposing)
            {
            }

            public Task InitializeAsync()
            {
                EnsureInitialized();

                return Task.CompletedTask;
            }
        }
    }
}
