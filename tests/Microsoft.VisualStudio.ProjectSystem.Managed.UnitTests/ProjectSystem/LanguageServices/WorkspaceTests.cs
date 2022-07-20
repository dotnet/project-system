// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Threading;

#pragma warning disable CA1068 // CancellationToken parameters must come last
#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
#pragma warning disable CS0618 // Type or member is obsolete

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

    [Theory]
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

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.WaitingForActivation, initializedTask.Status);

        await ApplyEvaluationAsync(workspace);

        Assert.Equal(0, callCount);
        Assert.Equal(TaskStatus.WaitingForActivation, initializedTask.Status);

        await ApplyBuildAsync(workspace);

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
        workspaceProjectContext.Setup(o => o.StartBatch());
        workspaceProjectContext.SetupSet(o => o.LastDesignTimeBuildSucceeded = false);
        workspaceProjectContext.Setup(o => o.SetOptions("CommandLineArgsForDesignTimeEvaluation"));
        workspaceProjectContext.Setup(o => o.EndBatchAsync()).Returns(() => new ValueTask());

        var evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            """
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "LanguageServiceName",
                            "TargetPath": "TargetPath",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                            "AssemblyName": "AssemblyName",
                            "CommandLineArgsForDesignTimeEvaluation": "CommandLineArgsForDesignTimeEvaluation"
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
                "LanguageServiceName",
                $"MSBuildProjectFullPath (net6.0 {projectGuid.ToString("B").ToUpperInvariant()})",
                "MSBuildProjectFullPath",
                projectGuid,
                hostObject,
                "TargetPath",
                "AssemblyName",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(delegate { return workspaceProjectContext.Object; });

        var workspace = await CreateInstanceAsync(
            unconfiguredProject: unconfiguredProject.Object,
            projectGuid: projectGuid,
            workspaceProjectContextFactory: workspaceProjectContextFactory.Object,
            evaluationRuleUpdate: evaluationRuleUpdate);

        workspaceProjectContextFactory.Verify();
        workspaceProjectContext.Verify();
        unconfiguredProjectServices.Verify();
        unconfiguredProject.Verify();

        Assert.Same(workspaceProjectContext.Object, workspace.Context);
    }

    [Theory]
    [CombinatorialData]
    public async Task EvaluationUpdate_IncompleteEvaluationDataFailsInitialization(bool empty1, bool empty2, bool empty3)
    {
        var evaluationRuleUpdate = IProjectSubscriptionUpdateFactory.FromJson(
            $$"""
            {
                "CurrentState": {
                    "ConfigurationGeneral": {
                        "Properties": {
                            "LanguageServiceName": "{{(empty1 ? "" : "LanguageServiceName")}}",
                            "TargetPath": "{{(empty2 ? "" : "TargetPath")}}",
                            "MSBuildProjectFullPath": "{{(empty3 ? "" : "MSBuildProjectFullPath")}}",
                            "AssemblyName": "AssemblyName",
                            "CommandLineArgsForDesignTimeEvaluation": "CommandLineArgsForDesignTimeEvaluation"
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

        var task = CreateInstanceAsync(evaluationRuleUpdate: evaluationRuleUpdate);

        if (!empty1 && !empty2 && !empty3)
        {
            await task;
        }
        else
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => task);

            Assert.Equal("Insufficient project data to initialize the language service.", ex.Message);
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
                            "TargetPath": "TargetPath",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                            "AssemblyName": "AssemblyName",
                            "CommandLineArgsForDesignTimeEvaluation": "CommandLineArgsForDesignTimeEvaluation"
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
                    It.IsAny<IProjectDiagnosticOutputService>()));
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
                            "TargetPath": "TargetPath",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                            "AssemblyName": "AssemblyName",
                            "CommandLineArgsForDesignTimeEvaluation": "CommandLineArgsForDesignTimeEvaluation"
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
                        It.IsAny<IProjectDiagnosticOutputService>()));
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
                        It.IsAny<IProjectDiagnosticOutputService>()));
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
            ? $$"""
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
            : $$"""
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
        updateHandler.As<IProjectEvaluationHandler>().SetupGet(o => o.ProjectEvaluationRule).Returns("MyEvaluationRule"); ;
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
                    It.IsAny<IProjectDiagnosticOutputService>()));
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
    public async Task ConstructionExceptionCleansUp()
    {
        var projectGuid = Guid.NewGuid();
        object hostObject = new();
        Exception ex = new("Error starting batch");

        Mock<UnconfiguredProjectServices> unconfiguredProjectServices = new(MockBehavior.Strict);
        unconfiguredProjectServices.SetupGet(o => o.HostObject).Returns(hostObject);

        Mock<UnconfiguredProject> unconfiguredProject = new(MockBehavior.Strict);
        unconfiguredProject.SetupGet(o => o.FullPath).Returns("""C:\MyProject\MyProject.csproj""");
        unconfiguredProject.SetupGet(o => o.Services).Returns(unconfiguredProjectServices.Object);

        Mock<IWorkspaceProjectContext> workspaceProjectContext = new(MockBehavior.Strict);
        workspaceProjectContext
            .Setup(o => o.StartBatch())
            .Throws(ex); // Throw straight away
        workspaceProjectContext.Setup(o => o.Dispose()); // Must be disposed

        Workspace workspace = await CreateInstanceAsync(
                unconfiguredProject: unconfiguredProject.Object,
                projectGuid: projectGuid,
                workspaceProjectContext: workspaceProjectContext.Object,
                applyEvaluation: false);

        IDataProgressTrackerServiceRegistration operationProgress = Mock.Of<IDataProgressTrackerServiceRegistration>();

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

        // An exception during context initialization leaves the context in a failed state.
        Assert.Same(ex, await Assert.ThrowsAsync<Exception>(() => ApplyEvaluationAsync(workspace, sourceItemsUpdate: sourceItemsUpdate)));

        workspaceProjectContext.Verify();
        unconfiguredProjectServices.Verify();
        unconfiguredProject.Verify();

        // Receiving another update will fail with a different exception (we cannot test the type as it's internal to vs-validation)
        Assert.NotSame(ex, await Assert.ThrowsAnyAsync<Exception>(() => ApplyEvaluationAsync(workspace, sourceItemsUpdate: sourceItemsUpdate)));

        // Attempting to write to a failed workspace produces the original exception
        Assert.Same(ex, await Assert.ThrowsAnyAsync<Exception>(() => workspace.WriteAsync(_ => Task.CompletedTask, CancellationToken.None)));

        Assert.Same(ex, await Assert.ThrowsAnyAsync<Exception>(() => workspace.WriteAsync(_ => Task.FromResult(123), CancellationToken.None)));
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
            (_, id, _, _, _, _, _, _) => { actualId = id; return Mock.Of<IWorkspaceProjectContext>(MockBehavior.Loose); });

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
        IProjectDiagnosticOutputService? logger = null,
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
        IProjectSubscriptionUpdate? buildRuleUpdate = null,
        CommandLineArgumentsSnapshot? commandLineArgumentsSnapshot = null)
    {
        var commandLineParserService = new Mock<ICommandLineParserService>(MockBehavior.Strict);
        commandLineParserService.Setup(o => o.Parse(It.IsAny<IEnumerable<string>>(), """C:\MyProject""")).Returns(EmptyBuildOptions);

        slice ??= ProjectConfigurationSlice.Create(ImmutableStringDictionary<string>.EmptyOrdinal.Add("TargetFramework", "net6.0"));
        unconfiguredProject ??= UnconfiguredProjectFactory.ImplementFullPath("""C:\MyProject\MyProject.csproj""");
        projectGuid ??= Guid.NewGuid();
        updateHandlers ??= new UpdateHandlers(Array.Empty<ExportFactory<IWorkspaceUpdateHandler>>());
        logger ??= IProjectDiagnosticOutputServiceFactory.Create();
        activeWorkspaceProjectContextTracker ??= IActiveEditorContextTrackerFactory.Create();
        commandLineParserServices ??= new(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst) { commandLineParserService.Object };
        dataProgressTrackerService ??= IDataProgressTrackerServiceFactory.Create();
        workspaceProjectContext ??= Mock.Of<IWorkspaceProjectContext>(MockBehavior.Loose);
        workspaceProjectContextFactory ??= IWorkspaceProjectContextFactoryFactory.ImplementCreateProjectContext(delegate { return workspaceProjectContext; });
        faultHandlerService ??= IProjectFaultHandlerServiceFactory.Create();
        joinableTaskFactory ??= new(new JoinableTaskCollection(new JoinableTaskContext()));
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
            await ApplyBuildAsync(workspace, configuredProject, buildRuleUpdate, commandLineArgumentsSnapshot);
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
                            "TargetPath": "TargetPath",
                            "MSBuildProjectFullPath": "MSBuildProjectFullPath",
                            "AssemblyName": "AssemblyName",
                            "CommandLineArgsForDesignTimeEvaluation": "CommandLineArgsForDesignTimeEvaluation"
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

        var update = WorkspaceUpdate.FromEvaluation((configuredProject, evaluationRuleUpdate, sourceItemsUpdate));

        await workspace.OnWorkspaceUpdateAsync(
            IProjectVersionedValueFactory.Create(update, ProjectDataSources.ConfiguredProjectVersion, configuredProjectVersion));
    }

    private static async Task ApplyBuildAsync(
        Workspace workspace,
        ConfiguredProject? configuredProject = null,
        IProjectSubscriptionUpdate? buildRuleUpdate = null,
        CommandLineArgumentsSnapshot? commandLineArgumentsSnapshot = null,
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

        commandLineArgumentsSnapshot ??= new(ImmutableArray<string>.Empty, isChanged: false);

        var update = WorkspaceUpdate.FromBuild((configuredProject, buildRuleUpdate, commandLineArgumentsSnapshot));

        await workspace.OnWorkspaceUpdateAsync(
            IProjectVersionedValueFactory.Create(update, ProjectDataSources.ConfiguredProjectVersion, configuredProjectVersion));
    }
}
