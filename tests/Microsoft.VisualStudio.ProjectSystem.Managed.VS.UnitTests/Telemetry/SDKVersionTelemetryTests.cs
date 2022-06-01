// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS;
using static Microsoft.VisualStudio.Telemetry.ITelemetryServiceFactory;

namespace Microsoft.VisualStudio.Telemetry
{
    public static class SDKVersionTelemetryTests
    {
        [Fact]
        public static async Task TestCreateComponentSDKVersionDefined()
        {
            var guid = Guid.NewGuid();
            var version = "42.42.42.42";
            var (success, result) = await CreateComponentAndGetResult(guid, version);
            Assert.NotNull(result);
            Assert.NotNull(result.Properties);
            Assert.True(success);
            Assert.Equal("vs/projectsystem/managed/sdkversion", result.EventName);
            Assert.Collection(result.Properties,
                args =>
                {
                    Assert.Equal("vs.projectsystem.managed.sdkversion.project", args.propertyName);
                    Assert.Equal(guid.ToString(), args.propertyValue as string);
                },
                args =>
                {
                    Assert.Equal("vs.projectsystem.managed.sdkversion.netcoresdkversion", args.propertyName);
                    Assert.Equal(version, args.propertyValue);
                });
        }

        [Fact]
        public static async Task TestCreateComponentSDKVersionDefinedInvalidProjectGuid()
        {
            var guid = Guid.Empty;
            var version = "42.42.42.42";
            var (success, _) = await CreateComponentAndGetResult(guid, version);
            Assert.False(success);
        }

        [Fact]
        public static async Task TestCreateComponentNoSDKVersionDefined()
        {
            var guid = Guid.NewGuid();
            var (success, _) = await CreateComponentAndGetResult(guid);
            Assert.False(success);
        }

        [Fact]
        public static async Task TestCreateComponentNoSDKVersionDefinedInvalidProjectGuid()
        {
            var guid = Guid.Empty;
            var (success, _) = await CreateComponentAndGetResult(guid);
            Assert.False(success);
        }

        private static async Task<(bool success, TelemetryParameters? result)> CreateComponentAndGetResult(Guid guid, string? version = null)
        {
            bool success = false;
            TelemetryParameters? result = null;
            void onTelemetryLogged(TelemetryParameters callParameters)
            {
                success = true;
                result = callParameters;
            }
            var component = CreateComponent(guid, onTelemetryLogged, version);
            await component.LoadAsync();
            await component.UnloadAsync();
            return (success, result);
        }

        private static SDKVersionTelemetryServiceComponent CreateComponent(Guid guid, Action<TelemetryParameters> onTelemetryLogged, string? version)
        {
            var projectVsServices = CreateProjectServices(version);
            var projectGuidService = CreateISafeProjectGuidService(guid);
            var telemetryService = CreateITelemetryService(onTelemetryLogged);
            var unconfiguredProjectTasksService = IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync(async t => await t());
            return new SDKVersionTelemetryServiceComponent(
                projectVsServices,
                projectGuidService,
                telemetryService,
                unconfiguredProjectTasksService);
        }

        private static IUnconfiguredProjectVsServices CreateProjectServices(string? version)
        {
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.NETCoreSdkVersionProperty, version ?? string.Empty);

            var projectProperties = ProjectPropertiesFactory.Create(project, data);

            return IUnconfiguredProjectVsServicesFactory.Implement(projectProperties: () => projectProperties);
        }

        private static ITelemetryService CreateITelemetryService(Action<TelemetryParameters> onTelemetryLogged) => Create(onTelemetryLogged);

        private static ISafeProjectGuidService CreateISafeProjectGuidService(Guid guid)
        {
            var mock = new Mock<ISafeProjectGuidService>();
            mock.Setup(s => s.GetProjectGuidAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(guid);

            return mock.Object;
        }
    }
}
