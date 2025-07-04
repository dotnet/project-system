﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.Debugger.UI.Interfaces.HotReload;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

public class LaunchProfilesDebugLaunchProviderTests
{
    private readonly Mock<IDebugProfileLaunchTargetsProvider> _mockWebProvider = new();
    private readonly Mock<IDebugProfileLaunchTargetsProvider> _mockDockerProvider = new();
    private readonly Mock<IDebugProfileLaunchTargetsProvider> _mockExeProvider = new();
    private readonly OrderPrecedenceImportCollection<IDebugProfileLaunchTargetsProvider> _launchProviders =
        new(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst);

    private readonly Mock<ConfiguredProject> _configuredProjectMoq = new();
    private readonly Mock<ILaunchSettingsProvider> _launchSettingsProviderMoq = new();
    private readonly List<IDebugLaunchSettings> _webProviderSettings = new();
    private readonly List<IDebugLaunchSettings> _dockerProviderSettings = new();
    private readonly List<IDebugLaunchSettings> _exeProviderSettings = new();

    // Set this to have ILaunchSettingsProvider return this profile (null by default)
    private ILaunchProfile? _activeProfile;

    public LaunchProfilesDebugLaunchProviderTests()
    {
        _mockWebProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => p.CommandName == "IISExpress");
        _mockWebProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => { return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_webProviderSettings); });
        _mockDockerProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => p.CommandName == "Docker");
        _mockDockerProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => { return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_dockerProviderSettings); });
        _mockExeProvider.Setup(x => x.SupportsProfile(It.IsAny<ILaunchProfile>())).Returns<ILaunchProfile>((p) => string.IsNullOrEmpty(p.CommandName) || p.CommandName == "Project");
        _mockExeProvider.Setup(x => x.QueryDebugTargetsAsync(It.IsAny<DebugLaunchOptions>(), It.IsAny<ILaunchProfile>())).Returns<DebugLaunchOptions, ILaunchProfile>((o, p) => { return Task.FromResult((IReadOnlyList<IDebugLaunchSettings>)_exeProviderSettings); });

        var mockMetadata = new Mock<IOrderPrecedenceMetadataView>();
        _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockWebProvider.Object, mockMetadata.Object));
        _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockDockerProvider.Object, mockMetadata.Object));
        _launchProviders.Add(new Lazy<IDebugProfileLaunchTargetsProvider, IOrderPrecedenceMetadataView>(() => _mockExeProvider.Object, mockMetadata.Object));

        _launchSettingsProviderMoq.Setup(x => x.ActiveProfile).Returns(() => _activeProfile);
        _launchSettingsProviderMoq.Setup(x => x.WaitForFirstSnapshot(It.IsAny<int>())).Returns(() =>
            _activeProfile is not null
                ? Task.FromResult((ILaunchSettings?)new LaunchSettings(new List<ILaunchProfile> { _activeProfile }, null, _activeProfile.Name))
                : Task.FromResult((ILaunchSettings?)LaunchSettings.Empty));
    }

    [Fact]
    public async Task CanLaunchAsyncTests()
    {
        var provider = CreateInstance();

        bool result = await provider.CanLaunchAsync(DebugLaunchOptions.NoDebug);
        Assert.True(result);
        result = await provider.CanLaunchAsync(0);
        Assert.True(result);
    }

    [Fact]
    public void GetLaunchTargetsProviderForProfileTestsAsync()
    {
        var provider = CreateInstance();
        Assert.Equal(_mockWebProvider.Object, provider.GetLaunchTargetsProvider(new LaunchProfile("test", "IISExpress")));
        Assert.Equal(_mockDockerProvider.Object, provider.GetLaunchTargetsProvider(new LaunchProfile("test", "Docker")));
        Assert.Equal(_mockExeProvider.Object, provider.GetLaunchTargetsProvider(new LaunchProfile("test", "Project")));
        Assert.Null(provider.GetLaunchTargetsProvider(new LaunchProfile("test", "IIS")));
    }

    [Fact]
    public async Task QueryDebugTargetsAsyncCorrectProvider()
    {
        var provider = CreateInstance();

        _activeProfile = new LaunchProfile("test", "IISExpress");
        var result = await provider.QueryDebugTargetsAsync(0);
        Assert.Equal(_webProviderSettings, result);

        _activeProfile = new LaunchProfile("test", "Docker");
        result = await provider.QueryDebugTargetsAsync(0);
        Assert.Equal(_dockerProviderSettings, result);

        _activeProfile = new LaunchProfile("test", "Project");
        result = await provider.QueryDebugTargetsAsync(0);
        Assert.Equal(_exeProviderSettings, result);
    }

    [Fact]
    public async Task QueryDebugTargetsAsync_WhenNoLaunchProfile_Throws()
    {
        var provider = CreateInstance();
        _activeProfile = null;

        await Assert.ThrowsAsync<Exception>(() => provider.QueryDebugTargetsAsync(0));
    }

    [Fact]
    public async Task QueryDebugTargetsAsync_WhenNoInstalledProvider_Throws()
    {
        var provider = CreateInstance();
        _activeProfile = new LaunchProfile("NoActionProfile", "SomeOtherExtension");

        await Assert.ThrowsAsync<Exception>(() => provider.QueryDebugTargetsAsync(0));
    }

    [Fact]
    public async Task LaunchWithProfileAsync_WhenHotReloadEnabled_CreatesHotReloadSession()
    {
        // Arrange
        var mockHotReloadSessionManager = Mock.Of<IProjectHotReloadSessionManager>();
        var mockHotReloadOptionService = IHotReloadOptionServiceFactory.Create();
        var mockVsDebuggerService = Mock.Of<IVsDebuggerLaunchAsync>();
        var mockProjectThreadingService = IProjectThreadingServiceFactory.Create();

        var provider = new LaunchProfilesDebugLaunchProvider(
            _configuredProjectMoq.Object,
            _launchSettingsProviderMoq.Object,
            new Lazy<IHotReloadOptionService>(() => mockHotReloadOptionService),
            new Lazy<IProjectHotReloadSessionManager>(() => mockHotReloadSessionManager),
            IVsServiceFactory.Create(mockVsDebuggerService),
            mockProjectThreadingService);

        provider.LaunchTargetsProviders.Add(_mockExeProvider.Object);

        var profile = new LaunchProfile("TestProfile", "Project", commandLineArgs: "--test");
        var launchOptions = DebugLaunchOptions.NoDebug;
        var debugLaunchSettings = new DebugLaunchSettings(launchOptions);
        debugLaunchSettings.Environment.Add("TEST_VAR", "test_value");

        // Set up the exe provider to return debug launch settings with environment
        _exeProviderSettings.Clear();
        _exeProviderSettings.Add(debugLaunchSettings);

        // Act
        await provider.LaunchWithProfileAsync(launchOptions, profile);

        // Assert - Verify Hot Reload session was created during launch
        Mock.Get(mockHotReloadSessionManager).Verify(
            manager => manager.TryCreatePendingSessionAsync(
                _configuredProjectMoq.Object,
                provider,
                debugLaunchSettings.Environment,
                launchOptions,
                profile),
            Times.Once);
    }

    [Fact]
    public async Task LaunchWithProfileAsync_WhenHotReloadDisabled_DoesNotCreateHotReloadSession()
    {
        // Arrange
        var mockHotReloadSessionManager = Mock.Of<IProjectHotReloadSessionManager>();
        var mockProjectThreadingService = IProjectThreadingServiceFactory.Create();

        var mockVsDebuggerService = Mock.Of<IVsDebuggerLaunchAsync>();
        var mockHotReloadOptionService = IHotReloadOptionServiceFactory.Create(false, false);
        var provider = new LaunchProfilesDebugLaunchProvider(
            _configuredProjectMoq.Object,
            _launchSettingsProviderMoq.Object,
            new Lazy<IHotReloadOptionService>(() => mockHotReloadOptionService),
            new Lazy<IProjectHotReloadSessionManager>(() => mockHotReloadSessionManager),
            IVsServiceFactory.Create(mockVsDebuggerService),
            mockProjectThreadingService);

        provider.LaunchTargetsProviders.Add(_mockExeProvider.Object);

        var profile = new LaunchProfile("TestProfile", "Project", commandLineArgs: "--test");
        var launchOptions = DebugLaunchOptions.NoDebug;
        var environment = new Dictionary<string, string?> { { "TEST_VAR", "test_value" } };

        // Set up the exe provider to return debug launch settings with environment
        _exeProviderSettings.Clear();
        var debugLaunchSettings = new DebugLaunchSettings(launchOptions);
        debugLaunchSettings.Environment.Add("TEST_VAR", "test_value");
        _exeProviderSettings.Add(debugLaunchSettings);

        // Act
        await provider.LaunchWithProfileAsync(launchOptions, profile);

        // Assert - Verify no Hot Reload session was created
        Mock.Get(mockHotReloadSessionManager).Verify(
            manager => manager.TryCreatePendingSessionAsync(
                It.IsAny<ConfiguredProject>(),
                It.IsAny<IProjectHotReloadLaunchProvider>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<DebugLaunchOptions>(),
                It.IsAny<ILaunchProfile>()),
            Times.Never);
    }

    [Fact]
    public async Task LaunchWithProfileAsync_WhenNotProjectCommand_DoesNotCreateHotReloadSession()
    {
        // Arrange
        var mockHotReloadSessionManager = Mock.Of<IProjectHotReloadSessionManager>();
        var mockHotReloadOptionService = IHotReloadOptionServiceFactory.Create();
        var mockVsDebuggerService = Mock.Of<IVsDebuggerLaunchAsync>();
        var mockProjectThreadingService = IProjectThreadingServiceFactory.Create();

        var provider = new LaunchProfilesDebugLaunchProvider(
            _configuredProjectMoq.Object,
            _launchSettingsProviderMoq.Object,
            new Lazy<IHotReloadOptionService>(() => mockHotReloadOptionService),
            new Lazy<IProjectHotReloadSessionManager>(() => mockHotReloadSessionManager),
            IVsServiceFactory.Create(mockVsDebuggerService),
            mockProjectThreadingService);

        provider.LaunchTargetsProviders.Add(_mockWebProvider.Object); // Use web provider instead

        var profile = new LaunchProfile("TestProfile", "IISExpress", commandLineArgs: "--test"); // Not a Project command
        var launchOptions = DebugLaunchOptions.NoDebug;
        var environment = new Dictionary<string, string?> { { "TEST_VAR", "test_value" } };

        // Set up the web provider to return debug launch settings with environment
        _webProviderSettings.Clear();
        var debugLaunchSettings = new DebugLaunchSettings(launchOptions);
        debugLaunchSettings.Environment.Add("TEST_VAR", "test_value");

        _webProviderSettings.Add(debugLaunchSettings);

        // Act
        await provider.LaunchWithProfileAsync(launchOptions, profile);

        // Assert - Verify no Hot Reload session was created for non-Project commands
        Mock.Get(mockHotReloadSessionManager).Verify(
            manager => manager.TryCreatePendingSessionAsync(
                It.IsAny<ConfiguredProject>(),
                It.IsAny<IProjectHotReloadLaunchProvider>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<DebugLaunchOptions>(),
                It.IsAny<ILaunchProfile>()),
            Times.Never);
    }

    private LaunchProfilesDebugLaunchProvider CreateInstance()
    {
        var provider = new LaunchProfilesDebugLaunchProvider(_configuredProjectMoq.Object, _launchSettingsProviderMoq.Object, hotReloadOptionSettings: null!, hotReloadSessionManager: null!, vsDebuggerService: null!);

        provider.LaunchTargetsProviders.Add(_mockWebProvider.Object);
        provider.LaunchTargetsProviders.Add(_mockDockerProvider.Object);
        provider.LaunchTargetsProviders.Add(_mockExeProvider.Object);

        return provider;
    }
}
