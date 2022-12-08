// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using Moq.Language.Flow;

#pragma warning disable CA1068 // CancellationToken parameters must come last
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

public class WorkspaceTests
{
    private static BuildOptions EmptyBuildOptions { get; } = new BuildOptions(
            sourceFiles: ImmutableArray<CommandLineSourceFile>.Empty,
            additionalFiles: ImmutableArray<CommandLineSourceFile>.Empty,
            metadataReferences: ImmutableArray<CommandLineReference>.Empty,
            analyzerReferences: ImmutableArray<CommandLineAnalyzerReference>.Empty,
            analyzerConfigFiles: ImmutableArray<string>.Empty);

    [Fact]
    public async Task Dispose_DisposesChainedDisposables()
    {
        var workspace = await CreateInstanceAsync();

        int disposedCount = 0;

        workspace.ChainDisposal(
            new DisposableDelegate(
                () => Interlocked.Increment(ref disposedCount)));

        Assert.Equal(0, disposedCount);

        await workspace.DisposeAsync();

        Assert.Equal(1, disposedCount);
    }

    [Fact]
    public async Task Dispose_ClearsPrimary()
    {
        var workspace = await CreateInstanceAsync();

        Assert.True(workspace.IsPrimary);

        await workspace.DisposeAsync();

        Assert.False(workspace.IsPrimary);
    }

    [Fact]
    public async Task Dispose_DisposesHandlers()
    {
        int disposeCount = 0;

        var exportFactory = ExportFactoryFactory.Implement(
            factory: () => Mock.Of<IWorkspaceUpdateHandler>(MockBehavior.Loose),
            disposeAction: () => disposeCount++);

        var workspace = await CreateInstanceAsync(updateHandlers: new UpdateHandlers(new[] { exportFactory }));

        Assert.Equal(0, disposeCount);

        await workspace.DisposeAsync();

        Assert.Equal(1, disposeCount);

        await workspace.DisposeAsync();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public async Task Dispose_TriggersObjectDisposedExceptionsOnPublicMembers()
    {
        var workspace = await CreateInstanceAsync();

        await workspace.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => workspace.WriteAsync(w => Task.CompletedTask, CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => workspace.WriteAsync(w => TaskResult.EmptyString, CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => workspace.OnWorkspaceUpdateAsync(null!));

        Assert.Throws<ObjectDisposedException>(() => workspace.ChainDisposal(null!));
    }

    [Theory]
    [CombinatorialData]
    public async Task WriteAsync_ThrowsIfNullAction(bool isGeneric)
    {
        var workspace = await CreateInstanceAsync(applyEvaluation: false);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => isGeneric
                ? workspace.WriteAsync<int>(null!, CancellationToken.None)
                : workspace.WriteAsync(null!, CancellationToken.None));
    }

    [Theory(Skip = "https://github.com/dotnet/project-system/issues/8592")]
    [CombinatorialData]
    public async Task WriteAsync_CompletesWhenDataReceived(bool isGeneric)
    {
        var workspace = await CreateInstanceAsync(applyEvaluation: false);

        int callCount = 0;

        Task initializedTask = isGeneric
            ? workspace.WriteAsync(
                w =>
                {
                    Assert.Same(workspace, w);
                    callCount++;

                    return Task.FromResult(1); // Task<T>
                },
                CancellationToken.None)
            : workspace.WriteAsync(
                w =>
                {
                    Assert.Same(workspace, w);
                    callCount++;

                    return Task.CompletedTask; // Task
                },
                CancellationToken.None);

        // Wait a little while to increase the chance a bug would surface in this test.
        await Task.Delay(10);

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.WaitingForActivation, initializedTask.Status);

        // Only once we have evaluation data should a write operation be scheduled.
        await ApplyEvaluationAsync(workspace);

        await Task.WhenAny(initializedTask, Task.Delay(TimeSpan.FromSeconds(30)));

        Assert.Equal(1, callCount);
        Assert.Equal(TaskStatus.RanToCompletion, initializedTask.Status);
    }

    [Theory]
    [CombinatorialData]
    public async Task WriteAsync_CancelledIfDisposedBeforeDataReceived(bool isGeneric)
    {
        var workspace = await CreateInstanceAsync(applyEvaluation: false);

        int callCount = 0;

        Task initializedTask = isGeneric
            ? workspace.WriteAsync(
                w =>
                {
                    Assert.Same(workspace, w);
                    callCount++;

                    return Task.FromResult(1); // Task<T>
                },
                CancellationToken.None)
            : workspace.WriteAsync(
                w =>
                {
                    Assert.Same(workspace, w);
                    callCount++;

                    return Task.CompletedTask; // Task
                },
                CancellationToken.None);

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.WaitingForActivation, initializedTask.Status);

        await workspace.DisposeAsync();

        await Task.WhenAny(initializedTask, Task.Delay(TimeSpan.FromSeconds(30)));

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.Canceled, initializedTask.Status);
    }

    [Theory]
    [CombinatorialData]
    public async Task WriteAsync_CancelledIfCancelledBeforeDataReceived(bool isGeneric)
    {
        var workspace = await CreateInstanceAsync(applyEvaluation: false);

        int callCount = 0;

        var cts = new CancellationTokenSource();

        Task initializedTask = isGeneric
            ? workspace.WriteAsync(
                w =>
                {
                    Assert.Same(workspace, w);
                    callCount++;

                    return Task.FromResult(1); // Task<T>
                },
                cts.Token)
            : workspace.WriteAsync(
                w =>
                {
                    Assert.Same(workspace, w);
                    callCount++;

                    return Task.CompletedTask; // Task
                },
                cts.Token);

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.WaitingForActivation, initializedTask.Status);

        cts.Cancel();

        await Task.WhenAny(initializedTask, Task.Delay(TimeSpan.FromSeconds(30)));

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.Canceled, initializedTask.Status);
    }

    [Fact]
    public async Task EvaluationUpdate_InitializesWorkspace()
    {
        var projectGuid = Guid.NewGuid();

        var hostObject = new object();

        Mock<UnconfiguredProjectServices> unconfiguredProjectServices = new(MockBehavior.Strict);
        unconfiguredProjectServices.SetupGet(o => o.HostObject).Returns(hostObject);

        Mock<UnconfiguredProject> unconfiguredProject = new(MockBehavior.Strict);
        unconfiguredProject.SetupGet(o => o.FullPath).Returns("""C:\MyProject\MyProject.csproj""");
        unconfiguredProject.SetupGet(o => o.Services).Returns(unconfiguredProjectServices.Object);

        var workspaceProjectContext = new Mock<IWorkspaceProjectContext>(MockBehavior.Strict);

        var evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            """
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "LanguageServiceName",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                        }
                    }
                },
                "ProjectChanges": {
                    "ConfigurationGeneral": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    }
                }
            }
            """);

        var workspaceProjectContextFactory = new Mock<IWorkspaceProjectContextFactory>(MockBehavior.Strict);
        workspaceProjectContextFactory.Setup(
            c => c.CreateProjectContextAsync(
                projectGuid,
                $"MSBuildProjectFullPath (net6.0 {projectGuid.ToString("B").ToUpperInvariant()})",
                "LanguageServiceName",
                It.IsAny<EvaluationData>(),
                hostObject,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegate { return workspaceProjectContext.Object; });

        var workspace = await CreateInstanceAsync(
            unconfiguredProject: unconfiguredProject.Object,
            projectGuid: projectGuid,
            workspaceProjectContextFactory: workspaceProjectContextFactory.Object,
            evaluationRuleUpdate: evaluationRuleUpdate);

        workspaceProjectContextFactory.VerifyAll();
        workspaceProjectContext.VerifyAll();
        unconfiguredProjectServices.VerifyAll();
        unconfiguredProject.VerifyAll();

        Assert.Same(workspaceProjectContext.Object, workspace.Context);
    }

    [Theory]
    [CombinatorialData]
    public async Task EvaluationUpdate_IncompleteEvaluationDataFailsInitialization(bool emptyLanguageService, bool emptyFullPath)
    {
        var evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            $$"""
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "{{(emptyLanguageService ? "" : "LanguageServiceName")}}",
                            "MSBuildProjectFullPath": "{{(emptyFullPath ? "" : "MSBuildProjectFullPath")}}",
                        }
                    }
                },
                "ProjectChanges": {
                    "ConfigurationGeneral": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    }
                }
            }
            """);

        var instance = await CreateInstanceAsync(evaluationRuleUpdate: evaluationRuleUpdate);

        if (!emptyLanguageService && !emptyFullPath)
        {
            Assert.False(instance.IsDisposed);
        }
        else
        {
            Assert.True(instance.IsDisposed);
        }
    }

    [Theory]
    [CombinatorialData]
    public async Task EvaluationUpdate_InvokesEvaluationHandlersWhenChangesExist(bool anyChanges)
    {
        var evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            $$"""
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "LanguageServiceName",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                        }
                    }
                },
                "ProjectChanges": {
                    "ConfigurationGeneral": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    },
                    "MyEvaluationRule": {
                        "Difference": {
                            "AnyChanges": {{anyChanges.ToString().ToLower()}}
                        }
                    }
                }
            }
            """);

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Loose);
        Mock<IWorkspaceUpdateHandler> updateHandler = new(MockBehavior.Strict);
        Mock<IProjectEvaluationHandler> projectEvaluationHandler = updateHandler.As<IProjectEvaluationHandler>();

        // Other handler kinds should be ignored
        updateHandler.As<ICommandLineHandler>();
        updateHandler.As<ISourceItemsHandler>();

        projectEvaluationHandler.SetupGet(o => o.ProjectEvaluationRule).Returns("MyEvaluationRule");

        if (anyChanges)
        {
            projectEvaluationHandler.Setup(
                o => o.Handle(
                    workspaceProjectContext.Object,
                    It.IsAny<ProjectConfiguration>(),
                    1,
                    It.IsAny<IProjectChangeDescription>(),
                    It.IsAny<ContextState>(),
                    It.IsAny<IManagedProjectDiagnosticOutputService>()));
        }

        var workspace = await CreateInstanceAsync(
            evaluationRuleUpdate: evaluationRuleUpdate,
            workspaceProjectContext: workspaceProjectContext.Object,
            updateHandlers: new UpdateHandlers(new[] { ExportFactoryFactory.Implement(() => updateHandler.Object) }));

        updateHandler.Verify();
    }

    [Theory]
    [CombinatorialData]
    public async Task EvaluationUpdate_InvokesProjectEvaluationHandlersWhenChangesExist(bool anyChanges)
    {
        var evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            $$"""
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "LanguageServiceName",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                        }
                    }
                },
                "ProjectChanges": {
                    "ConfigurationGeneral": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    },
                    "MyEvaluationRule": {
                        "Difference": {
                            "AnyChanges": {{anyChanges.ToString().ToLower()}}
                        }
                    }
                }
            }
            """);

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Loose);
        Mock<IWorkspaceUpdateHandler> updateHandler = new(MockBehavior.Strict);
        Mock<IProjectEvaluationHandler> projectEvaluationHandler = updateHandler.As<IProjectEvaluationHandler>();

        // Other handler kinds should be ignored
        updateHandler.As<ICommandLineHandler>();
        updateHandler.As<ISourceItemsHandler>();

        projectEvaluationHandler.SetupGet(o => o.ProjectEvaluationRule).Returns("MyEvaluationRule");

        if (anyChanges)
        {
            projectEvaluationHandler
                .Setup(
                    o => o.Handle(
                        workspaceProjectContext.Object,
                        It.IsAny<ProjectConfiguration>(),
                        1,
                        It.IsAny<IProjectChangeDescription>(),
                        new ContextState(false, true),
                        It.IsAny<IManagedProjectDiagnosticOutputService>()));
        }

        var workspace = await CreateInstanceAsync(
            evaluationRuleUpdate: evaluationRuleUpdate,
            workspaceProjectContext: workspaceProjectContext.Object,
            updateHandlers: new UpdateHandlers(new[] { ExportFactoryFactory.Implement(() => updateHandler.Object) }));

        updateHandler.Verify();
    }

    [Theory]
    [CombinatorialData]
    public async Task EvaluationUpdate_InvokesSourceItemHandlersWhenChangesExist(bool anyChanges)
    {
        var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            $$"""
            {
                "ProjectChanges": {
                    "RuleName": {
                        "Difference": {
                            "AnyChanges": {{anyChanges.ToString().ToLower()}}
                        }
                    }
                }
            }
            """);

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Loose);
        Mock<IWorkspaceUpdateHandler> updateHandler = new(MockBehavior.Strict);
        Mock<ISourceItemsHandler> sourceItemsHandler = updateHandler.As<ISourceItemsHandler>();

        // Other handler kinds should be ignored
        updateHandler.As<ICommandLineHandler>();
        updateHandler.As<IProjectEvaluationHandler>().SetupGet(o => o.ProjectEvaluationRule).Returns("MyEvaluationRule");;

        if (anyChanges)
        {
            sourceItemsHandler
                .As<ISourceItemsHandler>()
                .Setup(
                    o => o.Handle(
                        workspaceProjectContext.Object,
                        1,
                        It.IsAny<ImmutableDictionary<string, IProjectChangeDescription>>(),
                        new ContextState(false, true),
                        It.IsAny<IManagedProjectDiagnosticOutputService>()));
        }

        var workspace = await CreateInstanceAsync(
            sourceItemsUpdate: sourceItemsUpdate,
            workspaceProjectContext: workspaceProjectContext.Object,
            updateHandlers: new UpdateHandlers(new[] { ExportFactoryFactory.Implement(() => updateHandler.Object) }));

        updateHandler.Verify();
    }

    [Theory]
    [CombinatorialData]
    public async Task BuildUpdate_InvokesCommandLineHandlerWhenChangesExist(bool anyChanges)
    {
        var buildRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            anyChanges
            ? """
              {
                  "ProjectChanges": {
                      "CompilerCommandLineArgs": {
                          "Difference": {
                              "AnyChanges": true,
                              "AddedItems" : [ "/reference:Added.dll" ],
                              "RemovedItems" : [ "/reference:Removed.dll" ]
                          }
                      }
                  }
              }
              """
            : """
              {
                  "ProjectChanges": {
                      "CompilerCommandLineArgs": {
                          "Difference": {
                              "AnyChanges": false
                          }
                      }
                  }
              }
              """);

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Loose);
        Mock<IWorkspaceUpdateHandler> updateHandler = new(MockBehavior.Strict);
        Mock<ICommandLineHandler> commandLineHandler = updateHandler.As<ICommandLineHandler>();

        // Other handler kinds should be ignored
        updateHandler.As<IProjectEvaluationHandler>().SetupGet(o => o.ProjectEvaluationRule).Returns("MyEvaluationRule");
        updateHandler.As<ISourceItemsHandler>();

        if (anyChanges)
        {
            commandLineHandler.Setup(
                o => o.Handle(
                    workspaceProjectContext.Object,
                    1,
                    It.Is<BuildOptions>(options => options.MetadataReferences.Select(r => r.Reference).SingleOrDefault() == "Added.dll"),
                    It.Is<BuildOptions>(options => options.MetadataReferences.Select(r => r.Reference).SingleOrDefault() == "Removed.dll"),
                    new ContextState(false, true),
                    It.IsAny<IManagedProjectDiagnosticOutputService>()));
        }

        var parser = ICommandLineParserServiceFactory.CreateCSharp();

        var commandLineParserServices = new OrderPrecedenceImportCollection<ICommandLineParserService>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst)
        {
            new Lazy<ICommandLineParserService, IOrderPrecedenceMetadataView>(
                valueFactory: () => parser,
                metadata: IOrderPrecedenceMetadataViewFactory.Create())
        };

        var workspace = await CreateInstanceAsync(
            applyBuild: true,
            buildRuleUpdate: buildRuleUpdate,
            commandLineParserServices: commandLineParserServices,
            workspaceProjectContext: workspaceProjectContext.Object,
            updateHandlers: new UpdateHandlers(new[] { ExportFactoryFactory.Implement(() => updateHandler.Object) }));

        updateHandler.Verify();
    }

    [Fact]
    public async Task BuildUpdate_SetsLastDesignTimeBuildSucceeded()
    {
        var workspace = await CreateInstanceAsync();

        Assert.False(workspace.Context.LastDesignTimeBuildSucceeded);

        await ApplyBuildAsync(
            workspace,
            buildRuleUpdate: IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "ProjectChanges": {
                        "CompilerCommandLineArgs": {
                            "Difference": {
                                "AnyChanges": true
                            },
                            "After": {
                                "EvaluationSucceeded": true
                            }
                        }
                    }
                }
                """));

        Assert.True(workspace.Context.LastDesignTimeBuildSucceeded);

        await ApplyBuildAsync(
            workspace,
            buildRuleUpdate: IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "ProjectChanges": {
                        "CompilerCommandLineArgs": {
                            "Difference": {
                                "AnyChanges": true
                            },
                            "After": {
                                "EvaluationSucceeded": false
                            }
                        }
                    }
                }
                """));

        Assert.False(workspace.Context.LastDesignTimeBuildSucceeded);
    }

    [Fact]
    public async Task Update_StartBatchThrows()
    {
        Exception ex = new("Error starting batch");

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Strict);

        // StartBatch throws
        workspaceProjectContext.Setup(o => o.StartBatch()).Throws(ex);

        // Expect disposal
        workspaceProjectContext.Setup(o => o.Dispose());

        await EvaluationUpdateThrowsAndDisposesAsync(createProjectContext => createProjectContext.Returns(Task.FromResult(workspaceProjectContext.Object)), ex);

        workspaceProjectContext.VerifyAll();
    }

    [Fact]
    public async Task Update_EndBatchAsyncThrows()
    {
        Exception ex = new("Error starting batch");

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Strict);

        // EndBatchAsync throws
        workspaceProjectContext.Setup(o => o.StartBatch());
        workspaceProjectContext.Setup(o => o.EndBatchAsync()).Throws(ex);

        // Expect disposal
        workspaceProjectContext.Setup(o => o.Dispose());

        await EvaluationUpdateThrowsAndDisposesAsync(createProjectContext => createProjectContext.Returns(Task.FromResult(workspaceProjectContext.Object)), ex);

        workspaceProjectContext.VerifyAll();
    }

    [Fact]
    public async Task ContextInitialization_CreateProjectContextAsyncThrows()
    {
        Exception ex = new("Error creating project context");

        // CreateProjectContextAsync throws
        await EvaluationUpdateThrowsAndDisposesAsync(createProjectContext => createProjectContext.Throws(ex), ex);
    }

    [Fact]
    public async Task Fault_BeforeInitialisation()
    {
        var workspace = await CreateInstanceAsync(applyEvaluation: false);

        Task task = workspace.WriteAsync(_ => throw new Exception(), CancellationToken.None);

        Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

        var exception = new Exception();

        workspace.Fault(exception);

        var actualException = await Assert.ThrowsAsync<Exception>(() => task);

        Assert.Same(exception, actualException);
    }

    [Fact]
    public async Task Fault_AfterInitialisation()
    {
        // Initialised after this call (as we apply evaluation data)
        var workspace = await CreateInstanceAsync(applyEvaluation: true);

        int count = 0;

        await workspace.WriteAsync(async _ => count++, CancellationToken.None);

        Assert.Equal(1, count);

        // Faulting once initialised won't stop callers from using the workspace.
        // It only means we won't keep the workspace up to date over time as the project
        // changes.
        workspace.Fault(new Exception());

        await workspace.WriteAsync(async _ => count++, CancellationToken.None);

        Assert.Equal(2, count);
    }

    /// <summary>
    /// Template for actions on a newly constructed workspace that receives an exception during the initial update.
    /// Such exceptions should dispose the workspace and propagate to the caller.
    /// These are product bugs. The exceptions escape and produce NFEs.
    /// </summary>
    private async Task EvaluationUpdateThrowsAndDisposesAsync(Action<ISetup<IWorkspaceProjectContextFactory, Task<IWorkspaceProjectContext>>> createContext, Exception exception)
    {
        Mock<UnconfiguredProjectServices> unconfiguredProjectServices = new(MockBehavior.Strict);
        unconfiguredProjectServices.SetupGet(o => o.HostObject).Returns(new object());

        Mock<UnconfiguredProject> unconfiguredProject = new(MockBehavior.Strict);
        unconfiguredProject.SetupGet(o => o.FullPath).Returns("""C:\MyProject\MyProject.csproj""");
        unconfiguredProject.SetupGet(o => o.Services).Returns(unconfiguredProjectServices.Object);

        var workspaceProjectContextFactory = new Mock<IWorkspaceProjectContextFactory>(MockBehavior.Strict);
        createContext(workspaceProjectContextFactory.SetupCreateProjectContext());

        Workspace workspace = await CreateInstanceAsync(
                unconfiguredProject: unconfiguredProject.Object,
                projectGuid: Guid.NewGuid(),
                workspaceProjectContextFactory: workspaceProjectContextFactory.Object,
                applyEvaluation: false);

        var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                    "ProjectChanges": {
                        "RuleName": {
                            "Difference": {
                                "AnyChanges": true
                            }
                        }
                    }
                }
                """);

        Assert.False(workspace.IsDisposed);

        // Exceptions should propagate to the caller
        Assert.Same(exception, await Assert.ThrowsAsync<Exception>(() => ApplyEvaluationAsync(workspace, sourceItemsUpdate: sourceItemsUpdate)));

        // Exceptions should leave the workspace disposed
        Assert.True(workspace.IsDisposed);

        workspaceProjectContextFactory.VerifyAll();
        unconfiguredProjectServices.VerifyAll();
        unconfiguredProject.VerifyAll();
    }

    [Theory] // Configurations          Project GUID                               Expected
    [InlineData("Debug",                "{72B509BD-C502-4707-ADFD-E2D43867CF45}",  "MSBuildProjectFullPath ({72B509BD-C502-4707-ADFD-E2D43867CF45})")]
    [InlineData("Debug|AnyCPU",         "{72B509BD-C502-4707-ADFD-E2D43867CF45}",  "MSBuildProjectFullPath ({72B509BD-C502-4707-ADFD-E2D43867CF45})")]
    [InlineData("Debug|AnyCPU|net45",   "{72B509BD-C502-4707-ADFD-E2D43867CF45}",  "MSBuildProjectFullPath (net45 {72B509BD-C502-4707-ADFD-E2D43867CF45})")]
    public async Task CreateProjectContextAsync_UniquelyIdentifiesContext(string configuration, string guid, string expectedId)
    {
        var projectGuidService = ISafeProjectGuidServiceFactory.ImplementGetProjectGuidAsync(new Guid(guid));

        string? actualId = null;

        var workspaceProjectContextFactory = IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext(
            (_, id, _, _, _, _) => { actualId = id; return Mock.Of<IWorkspaceProjectContext>(MockBehavior.Loose); });

        var sliceValues = ProjectConfigurationFactory.Create(configuration).Dimensions
            .Remove(ConfigurationGeneral.ConfigurationProperty)
            .Remove(ConfigurationGeneral.PlatformProperty);

        var slice = ProjectConfigurationSlice.Create(sliceValues);

        var provider = await CreateInstanceAsync(
            slice: slice,
            workspaceProjectContextFactory: workspaceProjectContextFactory,
            projectGuid: new Guid(guid));

        Assert.Equal(expectedId, actualId);
    }

    private static async Task<Workspace> CreateInstanceAsync(
        ProjectConfigurationSlice? slice = null,
        UnconfiguredProject? unconfiguredProject = null,
        Guid? projectGuid = null,
        UpdateHandlers? updateHandlers = null,
        bool isPrimary = true,
        IManagedProjectDiagnosticOutputService? logger = null,
        IActiveEditorContextTracker? activeWorkspaceProjectContextTracker = null,
        OrderPrecedenceImportCollection<ICommandLineParserService>? commandLineParserServices = null,
        IDataProgressTrackerService? dataProgressTrackerService = null,
        IWorkspaceProjectContextFactory? workspaceProjectContextFactory = null,
        IWorkspaceProjectContext? workspaceProjectContext = null,
        IProjectFaultHandlerService? faultHandlerService = null,
        JoinableTaskFactory? joinableTaskFactory = null,
        JoinableTaskContextNode? joinableTaskContextNode = null,
        CancellationToken unloadCancellationToken = default,
        bool applyEvaluation = true,
        ConfiguredProject? configuredProject = null,
        IProjectSubscriptionUpdate? evaluationRuleUpdate = null,
        IProjectSubscriptionUpdate? sourceItemsUpdate = null,
        bool applyBuild = false,
        IProjectSubscriptionUpdate? buildRuleUpdate = null)
    {
        var commandLineParserService = new Mock<ICommandLineParserService>(MockBehavior.Strict);
        commandLineParserService.Setup(o => o.Parse(It.IsAny<IEnumerable<string>>(), """C:\MyProject""")).Returns(EmptyBuildOptions);

        slice ??= ProjectConfigurationSlice.Create(ImmutableStringDictionary<string>.EmptyOrdinal.Add("TargetFramework", "net6.0"));
        unconfiguredProject ??= UnconfiguredProjectFactory.ImplementFullPath("""C:\MyProject\MyProject.csproj""");
        projectGuid ??= Guid.NewGuid();
        updateHandlers ??= new UpdateHandlers(Array.Empty<ExportFactory<IWorkspaceUpdateHandler>>());
        logger ??= IManagedProjectDiagnosticOutputServiceFactory.Create();
        activeWorkspaceProjectContextTracker ??= IActiveEditorContextTrackerFactory.Create();
        commandLineParserServices ??= new(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst) { commandLineParserService.Object };
        dataProgressTrackerService ??= IDataProgressTrackerServiceFactory.Create();
        workspaceProjectContext ??= Mock.Of<IWorkspaceProjectContext>(MockBehavior.Loose);
        workspaceProjectContextFactory ??= IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext(delegate { return workspaceProjectContext; });
        faultHandlerService ??= IProjectFaultHandlerServiceFactory.Create();
#pragma warning disable VSSDK005
        JoinableTaskCollection joinableTaskCollection = new(new JoinableTaskContext());
#pragma warning restore VSSDK005
        joinableTaskFactory ??= new(joinableTaskCollection);
        joinableTaskContextNode ??= JoinableTaskContextNodeFactory.Create();

        var workspace = new Workspace(
            slice,
            unconfiguredProject,
            projectGuid.Value,
            updateHandlers,
            logger,
            activeWorkspaceProjectContextTracker,
            commandLineParserServices,
            dataProgressTrackerService,
            new(() => workspaceProjectContextFactory),
            faultHandlerService,
            joinableTaskCollection,
            joinableTaskFactory,
            joinableTaskContextNode,
            unloadCancellationToken)
        {
            IsPrimary = isPrimary
        };

        if (applyEvaluation)
        {
            await ApplyEvaluationAsync(workspace, configuredProject, evaluationRuleUpdate, sourceItemsUpdate);
        }

        if (applyBuild)
        {
            await ApplyBuildAsync(workspace, configuredProject, buildRuleUpdate);
        }

        return workspace;
    }

    private static async Task ApplyEvaluationAsync(
        Workspace workspace,
        ConfiguredProject? configuredProject = null,
        IProjectSubscriptionUpdate? evaluationRuleUpdate = null,
        IProjectSubscriptionUpdate? sourceItemsUpdate = null,
        int configuredProjectVersion = 1)
    {
        configuredProject ??= ConfiguredProjectFactory.Create();

        evaluationRuleUpdate ??= IProjectSubscriptionUpdateFactory.FromJson(
            """
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "LanguageServiceName",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                        }
                    }
                },
                "ProjectChanges": {
                    "ConfigurationGeneral": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    },
                    "MyEvaluationRule": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    }
                }
            }
            """);

        sourceItemsUpdate ??= IProjectSubscriptionUpdateFactory.FromJson(
            """
            {
                "ProjectChanges": {
                    "RuleName": {
                        "Difference": {
                            "AnyChanges": false
                        }
                    }
                }
            }
            """);

        IProjectSnapshot projectSnapshot = IProjectSnapshot2Factory.Create();

        var update = WorkspaceUpdate.FromEvaluation((configuredProject, projectSnapshot, evaluationRuleUpdate, sourceItemsUpdate));

        await workspace.OnWorkspaceUpdateAsync(
            IProjectVersionedValueFactory.Create(update, ProjectDataSources.ConfiguredProjectVersion, configuredProjectVersion));
    }

    private static async Task ApplyBuildAsync(
        Workspace workspace,
        ConfiguredProject? configuredProject = null,
        IProjectSubscriptionUpdate? buildRuleUpdate = null,
        int configuredProjectVersion = 1)
    {
        configuredProject ??= ConfiguredProjectFactory.Create();

        buildRuleUpdate ??= IProjectSubscriptionUpdateFactory.FromJson(
            """
            {
                "ProjectChanges": {
                    "RuleName": {
                        "Difference": {
                            "AnyChanges": true
                        },
                    },
                    "CompilerCommandLineArgs": {
                        "Difference": {
                            "AnyChanges": true
                        },
                    }
                }
            }
            """);

        var update = WorkspaceUpdate.FromBuild((configuredProject, buildRuleUpdate));

        await workspace.OnWorkspaceUpdateAsync(
            IProjectVersionedValueFactory.Create(update, ProjectDataSources.ConfiguredProjectVersion, configuredProjectVersion));
    }
}
