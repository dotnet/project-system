// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Telemetry;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependencyTreeTelemetryServiceTests
    {
        private const string TestFilePath = @"C:\Path\To\Project.csproj";

        private static ITelemetryServiceFactory.TelemetryParameters CalledParameters;

        [Fact]
        public void ObserveUnresolvedRules_LeavesTreeUpdateUnresolved()
        {
            var telemetryService = CreateInstance();

            telemetryService.ObserveUnresolvedRules(
                ITargetFrameworkFactory.Implement("tfm1"), 
                new string[] { "Rule1", "Rule2" });

            telemetryService.ObserveTreeUpdateCompleted();

            Assert.Equal("TreeUpdated/Unresolved", CalledParameters.EventName);
        }

        static DependencyTreeTelemetryService CreateInstance()
        {
            CalledParameters = new ITelemetryServiceFactory.TelemetryParameters();

            return new DependencyTreeTelemetryService(
                UnconfiguredProjectFactory.Create(filePath: TestFilePath), 
                ITelemetryServiceFactory.Create(CalledParameters));
        }
    }
}
