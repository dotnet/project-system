// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal partial class SDKVersionTelemetryServiceComponent
    {
        protected class SDKVersionTelemetryServiceInstance : AbstractProjectDynamicLoadInstance
        {
            private const string TelemetryEventName = "SDKVersion";
            private const string ProjectProperty = "Project";
            private const string NameProperty = "Name";
            private const string NETCoreSdkVersionProperty = "NETCoreSdkVersion";

            private readonly INETCoreSdkVersionProperty _sdkVersionProperty;
            private readonly ISafeProjectGuidService _projectGuidSevice;
            private readonly ITelemetryService _telemetryService;
            private readonly Action<NoSDKDetectedEventArgs> _onNoSDKDetected;

            [ImportingConstructor]
            public SDKVersionTelemetryServiceInstance(
                INETCoreSdkVersionProperty sdkVersionProperty,
                ISafeProjectGuidService projectGuidSevice,
                ITelemetryService telemetryService,
                IProjectThreadingService projectThreadingService,
                Action<NoSDKDetectedEventArgs> onNoSDKDetected)
                : base(projectThreadingService.JoinableTaskContext)
            {
                _sdkVersionProperty = sdkVersionProperty;
                _projectGuidSevice = projectGuidSevice;
                _telemetryService = telemetryService;
                _onNoSDKDetected = onNoSDKDetected;
            }

            protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
            {
                // Do not block initialization on reporting the sdk version. It is possible to deadlock.
                Task.Run(async () =>
                {
                    string version = await _sdkVersionProperty.GetValueAsync().ConfigureAwait(false);
                    string projectId = await GetProjectIdAsync().ConfigureAwait(false);

                    if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(projectId))
                    {
                        _onNoSDKDetected(new NoSDKDetectedEventArgs(projectId, version));
                        return;
                    }

                    _telemetryService.PostProperties(
                        TelemetryEventName,
                        new List<(string, object)>
                        {
                            (ProjectProperty, projectId),
                            (NETCoreSdkVersionProperty, version)
                        });
                });

                return Task.CompletedTask;
            }

            protected override Task DisposeCoreAsync(bool initialized) => Task.CompletedTask;

            private async Task<string> GetProjectIdAsync()
            {
                Guid projectGuid = await _projectGuidSevice.GetProjectGuidAsync().ConfigureAwait(false);
                return projectGuid == Guid.Empty ? null : projectGuid.ToString();
            }
        }
    }
}
