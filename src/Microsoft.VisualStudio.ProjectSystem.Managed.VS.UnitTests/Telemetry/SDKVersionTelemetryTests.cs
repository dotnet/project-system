// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Moq;
using Xunit;
using static Microsoft.VisualStudio.Telemetry.ITelemetryServiceFactory;

namespace Microsoft.VisualStudio.Telemetry
{
    [Trait("UnitTest", "ProjectSystem")]
    public class SDKVersionTelemetryTests
    {
        [Fact]
        public static async Task TestCreateCoponentSDKVersionDefined()
        {
            var guid = Guid.NewGuid();
            var version = "42.42.42.42";
            var (success, result) = await CreateComponentAndGetResult(guid, version);
            Assert.True(success);
            Assert.Equal("SDKVersion", result.EventName);
            Assert.Collection(result.Properties,
                args =>
                {
                    Assert.Equal("Project", args.propertyName);
                    Assert.Equal(guid.ToString(), args.propertyValue as string);
                },
                args =>
                {
                    Assert.Equal("NETCoreSdkVersion", args.propertyName);
                    Assert.Equal(version, args.propertyValue);
                });
        }

        [Fact]
        public static async Task TestCreateCoponentSDKVersionDefinedInvalidProjectGuid()
        {
            var guid = Guid.Empty;
            var version = "42.42.42.42";
            var (success, result) = await CreateComponentAndGetResult(guid, version);
            Assert.False(success);
            Assert.Equal("SDKVersion", result.EventName);
            Assert.Collection(result.Properties,
                args =>
                {
                    Assert.Equal("Project", args.propertyName);
                    Assert.Null(args.propertyValue);
                },
                args =>
                {
                    Assert.Equal("NETCoreSdkVersion", args.propertyName);
                    Assert.Equal(version, args.propertyValue);
                });
        }


        [Fact]
        public static async Task TestCreateCoponentNoSDKVersionDefined()
        {
            var guid = Guid.NewGuid();
            var (success, result) = await CreateComponentAndGetResult(guid);
            Assert.False(success);
            Assert.Equal("SDKVersion", result.EventName);
            Assert.Collection(result.Properties,
                args =>
                {
                    Assert.Equal("Project", args.propertyName);
                    Assert.Equal(guid.ToString(), args.propertyValue as string);
                },
                args =>
                {
                    Assert.Equal("NETCoreSdkVersion", args.propertyName);
                    Assert.Null(args.propertyValue);
                });
        }

        [Fact]
        public static async Task TestCreateCoponentNoSDKVersionDefinedInvalidProjectGuid()
        {
            var guid = Guid.Empty;
            var (success, result) = await CreateComponentAndGetResult(guid);
            Assert.False(success);
            Assert.Equal("SDKVersion", result.EventName);
            Assert.Collection(result.Properties,
                args =>
                {
                    Assert.Equal("Project", args.propertyName);
                    Assert.Null(args.propertyValue as string);
                },
                args =>
                {
                    Assert.Equal("NETCoreSdkVersion", args.propertyName);
                    Assert.Null(args.propertyValue);
                });
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
            var component = CreateCoponent(guid, onTelemetryLogged, version);
            component.OnNoSDKDetected += (s, e) =>
            {
                result = new TelemetryParameters
                {
                    EventName = "SDKVersion",
                    Properties = new List<(string, object)>
                    {
                        ("Project", e.ProjectGuid),
                        ("NETCoreSdkVersion", e.Version)
                    }
                };
                semaphore.Release();
            };
            await component.LoadAsync();
            await semaphore.WaitAsync();
            await component.UnloadAsync();
            return (success, result);
        }

        private static SDKVersionTelemetryServiceComponent CreateCoponent(Guid guid, Action<TelemetryParameters> onTelemetryLogged, string version)
        {
            var projectProperties = CreateProjectProperties(version);
            var projectGuidSevice = CreateISafeProjectGuidService(guid);
            var telemetryService = CreateITelemetryService(onTelemetryLogged);
            var projectThreadingService = new IProjectThreadingServiceMock();
            return new SDKVersionTelemetryServiceComponent(
                projectProperties,
                projectGuidSevice,
                telemetryService,
                projectThreadingService);
        }

        private static ITelemetryService CreateITelemetryService(Action<TelemetryParameters> onTelemetryLogged)
        {
            var telemetryService = new Mock<ITelemetryService>();

            telemetryService.Setup(t => t.PostEvent(It.IsAny<string>()))
                .Callback((string name) =>
                {
                    var callParameters = new TelemetryParameters
                    {
                        EventName = name
                    };
                    onTelemetryLogged(callParameters);
                });

            telemetryService.Setup(t => t.PostProperty(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Callback((string e, string p, object v) =>
                {
                    var callParameters = new TelemetryParameters
                    {
                        EventName = e,
                        Properties = new List<(string, object)>
                        {
                            (p, v)
                        }
                    };
                    onTelemetryLogged(callParameters);
                });

            telemetryService.Setup(t => t.PostProperties(It.IsAny<string>(), It.IsAny<IEnumerable<(string propertyName, object propertyValue)>>()))
                .Callback((string e, IEnumerable<(string propertyName, object propertyValue)> p) =>
                {
                    var callParameters = new TelemetryParameters
                    {
                        EventName = e,
                        Properties = p
                    };
                    onTelemetryLogged(callParameters);
                });

            return telemetryService.Object;
        }

        private static ISafeProjectGuidService CreateISafeProjectGuidService(Guid guid)
        {
            var mock = new Mock<ISafeProjectGuidService>();
            mock.Setup(s => s.GetProjectGuidAsync())
                .ReturnsAsync(guid);

            return mock.Object;
        }

        private static INETCoreSdkVersionProperty CreateProjectProperties(string version)
        {
            var mock = new Mock<INETCoreSdkVersionProperty>();
            mock.Setup(s => s.GetValueAsync())
                .ReturnsAsync(version);

            return mock.Object;
        }
    }
}
