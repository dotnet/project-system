// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Moq;
using Xunit;
using static Microsoft.VisualStudio.Telemetry.ITelemetryServiceFactory;

namespace Microsoft.VisualStudio.Telemetry
{
    public class SDKVersionTelemetryTests
    {
        [Fact]
        public static async Task TestCreateComponentSDKVersionDefined()
        {
            var guid = Guid.NewGuid();
            var version = "42.42.42.42";
            var (success, result) = await CreateComponentAndGetResult(guid, version);
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

        private static async Task<(bool success, TelemetryParameters result)> CreateComponentAndGetResult(Guid guid, string version = null)
        {
            var semaphore = new SemaphoreSlim(0);
            bool success = false;
            TelemetryParameters result = default;
            void onTelemetryLogged(TelemetryParameters callParameters)
            {
                success = true;
                result = callParameters;
                semaphore.Release();
            }
            var component = CreateComponent(guid, onTelemetryLogged, version, semaphore);
            await component.LoadAsync();
            await semaphore.WaitAsync();
            await component.UnloadAsync();
            return (success, result);
        }

        private static SDKVersionTelemetryServiceComponent CreateComponent(Guid guid, Action<TelemetryParameters> onTelemetryLogged, string version, SemaphoreSlim semaphore)
        {
            var projectVsServices = CreateProjectServices(version);
            var projectGuidService = CreateISafeProjectGuidService(guid);
            var telemetryService = CreateITelemetryService(onTelemetryLogged);
            var unconfiguredProjectTasksService = IUnconfiguredProjectTasksServiceFactory.ImplementLoadedProjectAsync(async t =>
            {
                await t();
                semaphore.Release();
            });
            return new SDKVersionTelemetryServiceComponent(
                projectVsServices,
                projectGuidService,
                telemetryService,
                unconfiguredProjectTasksService);
        }

        private static IUnconfiguredProjectVsServices CreateProjectServices(string version)
        {
            var setValues = new List<object>();
            var project = UnconfiguredProjectFactory.Create();
            var data = new PropertyPageData()
            {
                Category = ConfigurationGeneral.SchemaName,
                PropertyName = ConfigurationGeneral.NETCoreSdkVersionProperty,
                Value = version ?? string.Empty,
            };

            var projectProperties = ProjectPropertiesFactory.Create(project, data);
            var activeConfiguredProject = ActiveConfiguredProjectFactory.ImplementValue(() => projectProperties);

            return IUnconfiguredProjectVsServicesFactory.Implement(projectProperties: () => projectProperties);
        }

        private static ITelemetryService CreateITelemetryService(Action<TelemetryParameters> onTelemetryLogged) => Create(onTelemetryLogged);

        private static ISafeProjectGuidService CreateISafeProjectGuidService(Guid guid)
        {
            var mock = new Mock<ISafeProjectGuidService>();
            mock.Setup(s => s.GetProjectGuidAsync())
                .ReturnsAsync(guid);

            return mock.Object;
        }
    }
}
