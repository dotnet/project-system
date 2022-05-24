// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    public class ProjectHotReloadSessionManagerTests
    {
        [Fact]
        public async Task WhenActiveFrameworkMeetsRequirements_APendingSessionIsCreated()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" },
                { "DebugSymbols", "true" },
                // Note: "Optimize" is not included here. The compilers do not optimize by default;
                // so if the property isn't set that's OK.
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.True(sessionCreated);
        }

        [Fact]
        public async Task WhenTheSupportsHotReloadCapabilityIsMissing_APendingSessionIsNotCreated()
        {
            var capabilities = new[] { "ARandomCapabilityUnrelatedToHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" },
                { "DebugSymbols", "true" }
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.False(sessionCreated);
        }

        [Fact]
        public async Task WhenTheTargetFrameworkVersionIsNotDefined_APendingSessionIsNotCreated()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "ARandomProperty", "WithARandomValue" },
                { "DebugSymbols", "true" }
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.False(sessionCreated);
        }

        [Fact]
        public async Task WhenStartupHooksAreDisabled_APendingSessionIsNotCreated()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" },
                { "StartupHookSupport", "false" },
                { "DebugSymbols", "true" }
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            bool outputServiceCalled = false;
            void OutputServiceCallback() => outputServiceCalled = true;
            var manager = CreateHotReloadSessionManager(activeConfiguredProject, OutputServiceCallback);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.False(sessionCreated);
            Assert.True(outputServiceCalled);
        }

        [Fact]
        public async Task WhenOptimizationIsEnabled_APendingSessionIsNotCreated()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" },
                { "DebugSymbols", "true" },
                { "Optimize", "true" }
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.False(sessionCreated);
        }

        [Fact]
        public async Task WhenDebugSymbolsIsNotSpecified_APendingSessionIsNotCreated()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" }
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.False(sessionCreated);
        }

        [Fact]
        public async Task WhenDebugSymbolsIsFalse_APendingSessionIsNotCreated()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" },
                { "DebugSymbols", "false" }
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            var environmentVariables = new Dictionary<string, string>();
            var sessionCreated = await manager.TryCreatePendingSessionAsync(environmentVariables);

            Assert.False(sessionCreated);
        }

        [Fact]
        public void WhenNoActiveSession_HasSessionsReturnsFalse()
        {
            var capabilities = new[] { "SupportsHotReload" };
            var propertyNamesAndValues = new Dictionary<string, string?>()
            {
                { "TargetFrameworkVersion", "v6.0" },
                { "DebugSymbols", "true" },
                // Note: "Optimize" is not included here. The compilers do not optimize by default;
                // so if the property isn't set that's OK.
            };

            var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
            var manager = CreateHotReloadSessionManager(activeConfiguredProject);

            Assert.False(manager.HasActiveHotReloadSessions);
        }

        private static ProjectHotReloadSessionManager CreateHotReloadSessionManager(ConfiguredProject activeConfiguredProject, Action? outputServiceCallback = null)
        {
            var activeDebugFrameworkServices = new IActiveDebugFrameworkServicesMock()
                .ImplementGetConfiguredProjectForActiveFrameworkAsync(activeConfiguredProject)
                .Object;

            var manager = new ProjectHotReloadSessionManager(
                UnconfiguredProjectFactory.Create(),
                IProjectThreadingServiceFactory.Create(),
                IProjectFaultHandlerServiceFactory.Create(),
                activeDebugFrameworkServices,
                new Lazy<IProjectHotReloadAgent>(() => IProjectHotReloadAgentFactory.Create()),
                new Lazy<IHotReloadDiagnosticOutputService>(() => IHotReloadDiagnosticOutputServiceFactory.Create(outputServiceCallback)));

            return manager;
        }

        private static ConfiguredProject CreateConfiguredProject(string[] capabilities, Dictionary<string, string?> propertyNamesAndValues)
        {
            return ConfiguredProjectFactory.Create(
                IProjectCapabilitiesScopeFactory.Create(capabilities),
                services: ConfiguredProjectServicesFactory.Create(
                    projectPropertiesProvider: IProjectPropertiesProviderFactory.Create(
                        commonProps: IProjectPropertiesFactory.CreateWithPropertiesAndValues(
                            propertyNamesAndValues))));
        }
    }
}
