// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using IProjectCapabilitiesScopeFactory = Microsoft.VisualStudio.ProjectSystem.VS.IProjectCapabilitiesScopeFactory;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

public class ProjectHotReloadSessionManagerTests
{
    [Fact]
    public async Task WhenActiveFrameworkMeetsRequirements_APendingSessionIsCreated()
    {
        var capabilities = new[] { "SupportsHotReload" };
        var propertyNamesAndValues = new Dictionary<string, string?>()
        {
            { "TargetFramework", "net6.0" },
            { "DebugSymbols", "true" },
            // Note: "Optimize" is not included here. The compilers do not optimize by default;
            // so if the property isn't set that's OK.
        };

        var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
        var manager = CreateHotReloadSessionManager(activeConfiguredProject);

        var environmentVariables = new Dictionary<string, string>();
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

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
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

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
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

        Assert.False(sessionCreated);
    }

    [Fact]
    public async Task WhenStartupHooksAreDisabled_APendingSessionIsNotCreated()
    {
        var capabilities = new[] { "SupportsHotReload" };
        var propertyNamesAndValues = new Dictionary<string, string?>()
        {
            { "TargetFramework", "net6.0" },
            { "StartupHookSupport", "false" },
            { "DebugSymbols", "true" }
        };

        var activeConfiguredProject = CreateConfiguredProject(capabilities, propertyNamesAndValues);
        bool outputServiceCalled = false;
        void OutputServiceCallback() => outputServiceCalled = true;
        var manager = CreateHotReloadSessionManager(activeConfiguredProject, OutputServiceCallback);

        var environmentVariables = new Dictionary<string, string>();
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

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
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

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
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

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
        var launchOptions = DebugLaunchOptions.NoDebug;
        var launchProfile = CreateMockLaunchProfile();
        var launchProvider = IProjectHotReloadLaunchProviderFactory.Create();

        var sessionCreated = await manager.TryCreatePendingSessionAsync(
            launchProvider,
            environmentVariables,
            launchOptions,
            launchProfile);

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
        var manager = new ProjectHotReloadSessionManager(
            project: activeConfiguredProject,
            threadingService: IProjectThreadingServiceFactory.Create(),
            hotReloadDiagnosticOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => IHotReloadDiagnosticOutputServiceFactory.Create(outputServiceCallback)),
            projectHotReloadNotificationService: new Lazy<IProjectHotReloadNotificationService>(IProjectHotReloadNotificationServiceFactory.Create),
            buildManager: IProjectHotReloadBuildManagerFactory.Create(),
            launchProvider: IProjectHotReloadLaunchProviderFactory.Create(),
            hotReloadAgent: IProjectHotReloadAgentFactory.Create());

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

    private static ILaunchProfile CreateMockLaunchProfile()
    {
        var mock = new Mock<ILaunchProfile>();
        mock.Setup(p => p.Name).Returns("TestProfile");
        mock.Setup(p => p.CommandName).Returns("TestCommand");
        return mock.Object;
    }
}
