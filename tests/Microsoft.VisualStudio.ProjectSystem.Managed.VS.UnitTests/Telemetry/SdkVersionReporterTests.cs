// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Telemetry;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq.Protected;
using TelemetryParameters = Microsoft.VisualStudio.Telemetry.ITelemetryServiceFactory.TelemetryParameters;

namespace Microsoft.VisualStudio.Telemetry;

public static class SdkVersionReporterTests
{
    [Theory]
    [InlineData("42.42.42.42", true, true)]
    [InlineData("42.42.42.42", false, false)]
    [InlineData("", true, false)]
    [InlineData(null, true, false)]
    [InlineData("", false, false)]
    [InlineData(null, false, false)]
    public static async Task PublishesTelemetry(string? version, bool hasGuid, bool expectedSuccess)
    {
        Guid guid = hasGuid ? Guid.NewGuid() : Guid.Empty;

        int callCount = 0;
        TelemetryParameters? result = null;

        var projectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(
            projectProperties: () => ProjectPropertiesFactory.Create(
                project: UnconfiguredProjectFactory.Create(),
                data: new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.NETCoreSdkVersionProperty, version ?? string.Empty)));

        var mock = new Mock<SdkVersionReporter>(
            MockBehavior.Strict,
            projectVsServices,
            ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(guid),
            ITelemetryServiceFactory.Create(OnTelemetryLogged),
            IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync(t => t()));

        mock.Protected()
            .Setup<Task<string?>>("GetSdkVersionAsync")
            .Returns(Task.FromResult(version));

        // Act
        await mock.Object.LoadAsync();

        // Verify
        if (expectedSuccess)
        {
            Assert.Equal(1, callCount);
            Assert.NotNull(result);
            Assert.NotNull(result.Properties);
            Assert.Equal(TelemetryEventName.SDKVersion, result.EventName);
            Assert.Collection(
                result.Properties,
                property =>
                {
                    Assert.Equal(TelemetryPropertyName.SDKVersion.Project, property.Name);
                    Assert.Equal(guid.ToString(), property.Value);
                },
                property =>
                {
                    Assert.Equal(TelemetryPropertyName.SDKVersion.NETCoreSDKVersion, property.Name);
                    Assert.Equal(version, property.Value);
                });
        }
        else
        {
            Assert.Equal(0, callCount);
        }

        void OnTelemetryLogged(TelemetryParameters callParameters)
        {
            Interlocked.Increment(ref callCount);
            result = callParameters;
        }
    }
}
