// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

public class ProjectHotReloadSessionTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange
        string name = "TestSession";
        int variant = 42;

        // Act
        var session = CreateInstance(name: name, id: variant);

        // Assert
        Assert.Equal(name, session.Name);
    }

    [Fact]
    public async Task ApplyChangesAsync_WhenSessionActive_CallsHotReloadAgentManagerClient()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object));

        // Need to start the session to mark it as active
        await session.StartSessionAsync(CancellationToken.None);

        var cancellationToken = new CancellationToken();

        // Act
        await session.ApplyChangesAsync(cancellationToken);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.ApplyUpdatesAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ApplyChangesAsync_WhenSessionNotActive_DoesNotCallHotReloadAgentManagerClient()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object));

        // Session is not started/active

        // Act
        await session.ApplyChangesAsync(CancellationToken.None);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.ApplyUpdatesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyLaunchVariablesAsync_CallsDeltaApplier()
    {
        // Arrange
        var deltaApplier = new Mock<IDeltaApplier>();
        deltaApplier.Setup(d => d.ApplyProcessEnvironmentVariablesAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var callback = new Mock<IProjectHotReloadSessionCallback>();
        callback.Setup(c => c.GetDeltaApplier())
            .Returns(deltaApplier.Object);

        var session = CreateInstance(callback: callback.Object);

        var envVars = new Dictionary<string, string>
        {
            { "TEST_VAR", "TEST_VALUE" }
        };

        // Act
        bool result = await session.ApplyLaunchVariablesAsync(envVars, CancellationToken.None);

        // Assert
        Assert.True(result);
        deltaApplier.Verify(
            d => d.ApplyProcessEnvironmentVariablesAsync(envVars, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_InitializesSessionAndCallsAgentStarted()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var hotReloadOutputService = new Mock<IHotReloadDiagnosticOutputService>();

        var configuredProject = CreateConfiguredProjectWithCommonProperties();

        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object),
            hotReloadOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => hotReloadOutputService.Object),
            configuredProject: configuredProject);

        // Act
        await session.StartSessionAsync(CancellationToken.None);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.AgentStartedAsync(
                session,
                HotReloadAgentFlags.None,
                It.IsAny<ManagedEditAndContinueProcessInfo>(),
                It.IsAny<RunningProjectInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        hotReloadOutputService.Verify(
            service => service.WriteLine(
                It.IsAny<HotReloadLogMessage>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartSessionAsync_VerifiesCorrectProcessInfoAndProjectInfo()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var hotReloadOutputService = new Mock<IHotReloadDiagnosticOutputService>();

        // Create a project with a specific TFM and path
        const string expectedProjectPath = "C:\\Test\\Project.csproj";
        const string expectedTf = "net6.0";

        var configuredProject = CreateConfiguredProjectWithCommonProperties(expectedTf, expectedProjectPath);

        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object),
            hotReloadOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => hotReloadOutputService.Object),
            configuredProject: configuredProject);

        RunningProjectInfo? capturedRunningProjectInfo = null;
        ManagedEditAndContinueProcessInfo? capturedProcessInfo = null;

        hotReloadAgentManagerClient.Setup(client => client.AgentStartedAsync(
                It.IsAny<IManagedHotReloadAgent>(),
                It.IsAny<HotReloadAgentFlags>(),
                It.IsAny<ManagedEditAndContinueProcessInfo>(),
                It.IsAny<RunningProjectInfo>(),
                It.IsAny<CancellationToken>()))
            .Callback<IManagedHotReloadAgent, HotReloadAgentFlags, ManagedEditAndContinueProcessInfo, RunningProjectInfo, CancellationToken>(
                (_, _, processInfo, runningProjectInfo, _) =>
                {
                    capturedProcessInfo = processInfo;
                    capturedRunningProjectInfo = runningProjectInfo;
                })
            .Returns(new ValueTask());

        // Act
        await session.StartSessionAsync(CancellationToken.None);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.AgentStartedAsync(
                session,
                HotReloadAgentFlags.None,
                It.IsAny<ManagedEditAndContinueProcessInfo>(),
                It.IsAny<RunningProjectInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify the correct RunningProjectInfo was passed
        Assert.NotNull(capturedRunningProjectInfo);
        Assert.False(capturedRunningProjectInfo?.RestartAutomatically);
        Assert.NotNull(capturedRunningProjectInfo?.ProjectInstanceId);
        Assert.Equal(expectedProjectPath, capturedRunningProjectInfo?.ProjectInstanceId.ProjectFilePath);
        Assert.Equal(expectedTf, capturedRunningProjectInfo?.ProjectInstanceId.TargetFramework);

        // Verify processInfo was created correctly
        Assert.NotNull(capturedProcessInfo);
    }

    [Fact]
    public async Task StartSessionAsync_CallsAgentStartedWithCorrectParameters()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var configuredProject = CreateConfiguredProjectWithCommonProperties("net6.0", "C:\\Test\\Project.csproj");
        var cancellationToken = CancellationToken.None;

        RunningProjectInfo capturedProjectInfo = default;
        ManagedEditAndContinueProcessInfo capturedProcessInfo = default;
        HotReloadAgentFlags capturedFlags = HotReloadAgentFlags.None;

        hotReloadAgentManagerClient
            .Setup(client => client.AgentStartedAsync(
                It.IsAny<IManagedHotReloadAgent>(),
                It.IsAny<HotReloadAgentFlags>(),
                It.IsAny<ManagedEditAndContinueProcessInfo>(),
                It.IsAny<RunningProjectInfo>(),
                It.IsAny<CancellationToken>()))
            .Callback<IManagedHotReloadAgent, HotReloadAgentFlags, ManagedEditAndContinueProcessInfo, RunningProjectInfo, CancellationToken>(
                (agent, flags, processInfo, projectInfo, ct) =>
                {
                    capturedFlags = flags;
                    capturedProcessInfo = processInfo;
                    capturedProjectInfo = projectInfo;
                })
            .Returns(new ValueTask());

        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object),
            configuredProject: configuredProject);

        // Act
        await session.StartSessionAsync(cancellationToken);

        // Assert
        Assert.Equal(HotReloadAgentFlags.None, capturedFlags);

        // Verify RunningProjectInfo properties
        Assert.False(capturedProjectInfo.RestartAutomatically);
        Assert.Equal("C:\\Test\\Project.csproj", capturedProjectInfo.ProjectInstanceId.ProjectFilePath);
        Assert.Equal("net6.0", capturedProjectInfo.ProjectInstanceId.TargetFramework);

        // Verify AgentStartedAsync was called once
        hotReloadAgentManagerClient.Verify(
            client => client.AgentStartedAsync(
                session,
                HotReloadAgentFlags.None,
                It.IsAny<ManagedEditAndContinueProcessInfo>(),
                It.Is<RunningProjectInfo>(info =>
                    info.ProjectInstanceId.ProjectFilePath == "C:\\Test\\Project.csproj" &&
                    info.ProjectInstanceId.TargetFramework == "net6.0" &&
                    info.RestartAutomatically == false),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_WithDebugger_SetsCorrectFlags()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var configuredProject = CreateConfiguredProjectWithCommonProperties();
        var cancellationToken = CancellationToken.None;

        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object),
            configuredProject: configuredProject,
            debugLaunchOptions: 0);

        // Act
        await session.StartSessionAsync(cancellationToken);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.AgentStartedAsync(
                session,
                HotReloadAgentFlags.IsDebuggedProcess,
                It.IsAny<ManagedEditAndContinueProcessInfo>(),
                It.IsAny<RunningProjectInfo>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = CreateInstance();
        await session.StartSessionAsync(CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.StartSessionAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopSessionAsync_WhenSessionActive_CallsAgentTerminated()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var hotReloadOutputService = new Mock<IHotReloadDiagnosticOutputService>();

        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object),
            hotReloadOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => hotReloadOutputService.Object));

        await session.StartSessionAsync(CancellationToken.None);

        // Act
        await session.StopSessionAsync(CancellationToken.None);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.AgentTerminatedAsync(
                session,
                It.IsAny<CancellationToken>()),
            Times.Once);

        hotReloadOutputService.Verify(
            service => service.WriteLine(
                It.Is<HotReloadLogMessage>(m => m.Message == Resources.HotReloadStopSession),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopSessionAsync_WhenSessionNotActive_DoesNotCallAgentTerminated()
    {
        // Arrange
        var hotReloadAgentManagerClient = new Mock<IHotReloadAgentManagerClient>();
        var session = CreateInstance(
            hotReloadAgentManagerClient: new Lazy<IHotReloadAgentManagerClient>(() => hotReloadAgentManagerClient.Object));

        // Session is not started/active

        // Act
        await session.StopSessionAsync(CancellationToken.None);

        // Assert
        hotReloadAgentManagerClient.Verify(
            client => client.AgentTerminatedAsync(
                It.IsAny<IManagedHotReloadAgent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyUpdatesAsync_WhenSessionActiveAndDeltaApplierExists_CallsDeltaApplier()
    {
        // Arrange
        var deltaApplier = new Mock<IDeltaApplier>();
        deltaApplier.Setup(d => d.ApplyUpdatesAsync(It.IsAny<ImmutableArray<ManagedHotReloadUpdate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplyResult.Success);

        var callback = new Mock<IProjectHotReloadSessionCallback>();
        callback.Setup(c => c.GetDeltaApplier())
            .Returns(deltaApplier.Object);

        var session = CreateInstance(callback: callback.Object);

        await session.StartSessionAsync(CancellationToken.None);

        var updates = ImmutableArray.Create<ManagedHotReloadUpdate>();

        // Act
        await session.ApplyUpdatesAsync(updates, CancellationToken.None);

        // Assert
        deltaApplier.Verify(
            d => d.ApplyUpdatesAsync(updates, It.IsAny<CancellationToken>()),
            Times.Once);

        callback.Verify(
            c => c.OnAfterChangesAppliedAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyUpdatesAsync_WhenSessionNotActive_DoesNotCallDeltaApplier()
    {
        // Arrange
        var deltaApplier = new Mock<IDeltaApplier>();
        var callback = new Mock<IProjectHotReloadSessionCallback>();
        callback.Setup(c => c.GetDeltaApplier())
            .Returns(deltaApplier.Object);

        var session = CreateInstance(callback: callback.Object);
        // Session is not started/active

        var updates = ImmutableArray.Create<ManagedHotReloadUpdate>();

        // Act
        await session.ApplyUpdatesAsync(updates, CancellationToken.None);

        // Assert
        deltaApplier.Verify(
            d => d.ApplyUpdatesAsync(It.IsAny<ImmutableArray<ManagedHotReloadUpdate>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyUpdatesAsync_WhenDeltaApplierThrows_WritesToOutputAndRethrows()
    {
        // Arrange
        var hotReloadOutputService = new Mock<IHotReloadDiagnosticOutputService>();

        var deltaApplier = new Mock<IDeltaApplier>();
        deltaApplier.Setup(d => d.ApplyUpdatesAsync(It.IsAny<ImmutableArray<ManagedHotReloadUpdate>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var callback = new Mock<IProjectHotReloadSessionCallback>();
        callback.Setup(c => c.GetDeltaApplier())
            .Returns(deltaApplier.Object);

        var session = CreateInstance(
            hotReloadOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => hotReloadOutputService.Object),
            callback: callback.Object);

        await session.StartSessionAsync(CancellationToken.None);

        var updates = ImmutableArray.Create<ManagedHotReloadUpdate>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await session.ApplyUpdatesAsync(updates, CancellationToken.None));

        hotReloadOutputService.Verify(
            service => service.WriteLine(
                It.Is<HotReloadLogMessage>(m => m.ErrorLevel == HotReloadDiagnosticErrorLevel.Error),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReportDiagnosticsAsync_WritesDiagnosticsToOutput()
    {
        // Arrange
        var hotReloadOutputService = new Mock<IHotReloadDiagnosticOutputService>();
        var session = CreateInstance(
            hotReloadOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => hotReloadOutputService.Object));

        var diagnostics = ImmutableArray.Create(
            new ManagedHotReloadDiagnostic(
                id: "TestDiagnostic",
                message: "Test message",
                severity: ManagedHotReloadDiagnosticSeverity.Error,
                filePath: "Test.cs",
                span: new SourceSpan(1, 2, 3, 4))
            );

        // Act
        await session.ReportDiagnosticsAsync(diagnostics, CancellationToken.None);

        // Assert
        // Verify main error message
        hotReloadOutputService.Verify(
            service => service.WriteLine(
                It.Is<HotReloadLogMessage>(m =>
                    m.Message == Resources.HotReloadErrorsInApplication &&
                    m.ErrorLevel == HotReloadDiagnosticErrorLevel.Error),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify diagnostic message
        hotReloadOutputService.Verify(
            service => service.WriteLine(
                It.Is<HotReloadLogMessage>(m =>
                    m.Message.Contains(diagnostics[0].FilePath) &&
                    m.Message.Contains(diagnostics[0].Message) &&
                    m.ErrorLevel == HotReloadDiagnosticErrorLevel.Error),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_CallsStopProjectAsync()
    {
        // Arrange
        var hotReloadOutputService = new Mock<IHotReloadDiagnosticOutputService>();
        var callback = new Mock<IProjectHotReloadSessionCallback>();

        var session = CreateInstance(
            hotReloadOutputService: new Lazy<IHotReloadDiagnosticOutputService>(() => hotReloadOutputService.Object),
            callback: callback.Object);

        // Act
        await session.StopAsync(CancellationToken.None);

        // Assert
        callback.Verify(
            c => c.StopProjectAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        hotReloadOutputService.Verify(
            service => service.WriteLine(
                It.Is<HotReloadLogMessage>(m => m.Message == Resources.HotReloadStoppingApplication),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProjectFullPathAsync_ReturnsCorrectValue()
    {
        // Test case 1: With IProjectHotReloadSessionCallback2
        {
            // Arrange
            var project = new Mock<UnconfiguredProject>();
            project.SetupGet(p => p.FullPath)
                .Returns("C:\\Test\\Project.csproj");

            var callback = new Mock<IProjectHotReloadSessionCallback2>();
            callback.SetupGet(c => c.Project)
                .Returns(project.Object);

            var session = CreateInstance(callback: callback.Object);

            // Act
            var result = await session.GetProjectFullPathAsync(CancellationToken.None);

            // Assert
            Assert.Equal("C:\\Test\\Project.csproj", result);
        }

        // Test case 2: Without IProjectHotReloadSessionCallback2
        {
            // Arrange
            var callback = new Mock<IProjectHotReloadSessionCallback>();
            var session = CreateInstance(callback: callback.Object);

            // Act
            var result = await session.GetProjectFullPathAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task SupportsRestartAsync_WhenAllRequiredParametersPresent_ReturnsTrue()
    {
        // Arrange
        var launchProfile = new Mock<ILaunchProfile>().Object;
        var debugLaunchOptions = DebugLaunchOptions.NoDebug;
        var buildManager = new Mock<IProjectHotReloadBuildManager>().Object;
        var launchProvider = new Mock<IProjectHotReloadLaunchProvider>().Object;

        var session = CreateInstance(
            launchProfile: launchProfile,
            debugLaunchOptions: debugLaunchOptions,
            buildManager: buildManager,
            launchProvider: launchProvider);

        // Act
        bool result = await session.SupportsRestartAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    #region Test Helpers

    private static ProjectHotReloadSession CreateInstance(
        string name = "TestSession",
        int id = 0,
        Lazy<IHotReloadAgentManagerClient>? hotReloadAgentManagerClient = null,
        Lazy<IHotReloadDiagnosticOutputService>? hotReloadOutputService = null,
        Lazy<IManagedDeltaApplierCreator>? deltaApplierCreator = null,
        IProjectHotReloadSessionCallback? callback = null,
        IProjectHotReloadSessionManager? sessionManager = null,
        ConfiguredProject? configuredProject = null,
        ILaunchProfile? launchProfile = null,
        DebugLaunchOptions debugLaunchOptions = DebugLaunchOptions.NoDebug,
        IProjectHotReloadBuildManager? buildManager = null,
        IProjectHotReloadLaunchProvider? launchProvider = null)
    {
        hotReloadAgentManagerClient ??= new Lazy<IHotReloadAgentManagerClient>(() => Mock.Of<IHotReloadAgentManagerClient>());
        hotReloadOutputService ??= new Lazy<IHotReloadDiagnosticOutputService>(() => Mock.Of<IHotReloadDiagnosticOutputService>());

        var mockDeltaApplier = new Mock<IDeltaApplier>();
        mockDeltaApplier.Setup(d => d.ApplyProcessEnvironmentVariablesAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockDeltaApplier.Setup(d => d.ApplyUpdatesAsync(It.IsAny<ImmutableArray<ManagedHotReloadUpdate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplyResult.Success);
        mockDeltaApplier.Setup(d => d.GetCapabilitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var mockDeltaApplierCreator = new Mock<IManagedDeltaApplierCreator>();
        mockDeltaApplierCreator.Setup(c => c.CreateManagedDeltaApplier(It.IsAny<string>()))
            .Returns(mockDeltaApplier.Object);

        deltaApplierCreator ??= new Lazy<IManagedDeltaApplierCreator>(() => mockDeltaApplierCreator.Object);

        callback ??= Mock.Of<IProjectHotReloadSessionCallback>(c =>
            c.GetDeltaApplier() == mockDeltaApplier.Object &&
            c.StopProjectAsync(It.IsAny<CancellationToken>()) == Task.FromResult(true) &&
            c.OnAfterChangesAppliedAsync(It.IsAny<CancellationToken>()) == Task.CompletedTask);

        launchProfile ??= new Mock<ILaunchProfile>().Object;
        buildManager ??= new Mock<IProjectHotReloadBuildManager>().Object;
        launchProvider ??= new Mock<IProjectHotReloadLaunchProvider>().Object;
        configuredProject ??= CreateConfiguredProjectWithCommonProperties();

        return new ProjectHotReloadSession(
            name,
            id,
            hotReloadAgentManagerClient,
            hotReloadOutputService,
            deltaApplierCreator,
            callback,
            buildManager,
            launchProvider,
            configuredProject,
            launchProfile,
            debugLaunchOptions);
    }

    private static ConfiguredProject CreateConfiguredProjectWithCommonProperties(string targetFramework = "net6.0", string projectPath = "C:\\Test\\Project.csproj")
    {
        var commonProperties = new Mock<IProjectProperties>();
        commonProperties.Setup(p => p.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty))
            .ReturnsAsync(targetFramework);

        var projectPropertiesProvider = new Mock<IProjectPropertiesProvider>();
        projectPropertiesProvider.Setup(p => p.GetCommonProperties())
            .Returns(commonProperties.Object);

        var configuredProjectServices = new Mock<ConfiguredProjectServices>();
        configuredProjectServices.Setup(s => s.ProjectPropertiesProvider)
            .Returns(projectPropertiesProvider.Object);

        var unconfiguredProject = new Mock<UnconfiguredProject>();
        unconfiguredProject.Setup(p => p.FullPath)
            .Returns(projectPath);

        var configuredProject = new Mock<ConfiguredProject>();
        configuredProject.Setup(c => c.Services)
            .Returns(configuredProjectServices.Object);
        configuredProject.Setup(c => c.UnconfiguredProject)
            .Returns(unconfiguredProject.Object);

        return configuredProject.Object;
    }

    #endregion
}
