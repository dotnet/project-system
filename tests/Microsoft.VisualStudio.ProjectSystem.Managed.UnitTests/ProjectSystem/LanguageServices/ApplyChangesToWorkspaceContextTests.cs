// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class ApplyChangesToWorkspaceContextTests
    {
        private readonly CommandLineArgumentsSnapshot _unchangedCommandLineArguments = new(ImmutableArray.Create("A", "B"), isChanged: false);
        private readonly CommandLineArgumentsSnapshot _changedCommandLineArguments = new(ImmutableArray.Create("A", "B"), isChanged: true);

        [Fact]
        public async Task DisposeAsync_WhenNotInitialized_DoesNotThrow()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            Assert.True(applyChangesToWorkspace.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_WhenInitializedWithNoHandlers_DoesNotThrow()
        {
            var applyChangesToWorkspace = CreateInstance();
            var context = IWorkspaceProjectContextMockFactory.Create();
            applyChangesToWorkspace.Initialize(context);

            await applyChangesToWorkspace.DisposeAsync();

            Assert.True(applyChangesToWorkspace.IsDisposed);
        }

        [Fact]
        public async Task DisposeAsync_DisposesHandlers()
        {
            int callCount = 0;
            var handler = IWorkspaceContextHandlerFactory.ImplementDispose(() => { callCount++; });

            var applyChangesToWorkspace = CreateInstance(handlers: handler);
            var context = IWorkspaceProjectContextMockFactory.Create();

            applyChangesToWorkspace.Initialize(context);

            await applyChangesToWorkspace.DisposeAsync();

            Assert.Equal(1, callCount);
        }

        [Fact]
        public void Initialize_NullAsContext_ThrowsArgumentNull()
        {
            var applyChangesToWorkspace = CreateInstance();

            Assert.Throws<ArgumentNullException>("context", () =>
            {
                applyChangesToWorkspace.Initialize(null!);
            });
        }

        [Fact]
        public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();
            var context = IWorkspaceProjectContextMockFactory.Create();

            applyChangesToWorkspace.Initialize(context);

            Assert.Throws<InvalidOperationException>(() =>
            {
                applyChangesToWorkspace.Initialize(context);
            });
        }

        [Fact]
        public void GetProjectEvaluationRules_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();

            Assert.Throws<InvalidOperationException>(() =>
            {
                applyChangesToWorkspace.GetProjectEvaluationRules();
            });
        }

        [Fact]
        public void GetDesignTimeRules_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();

            Assert.Throws<InvalidOperationException>(() =>
            {
                applyChangesToWorkspace.GetProjectBuildRules();
            });
        }

        [Fact]
        public void ApplyProjectEvaluation_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();

            var update = Mock.Of<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, IProjectSubscriptionUpdate SourceItemsUpdate)>>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(), CancellationToken.None);
            });
        }

        [Fact]
        public void ApplyProjectBuild_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();

            var update = Mock.Of<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, CommandLineArgumentsSnapshot CommandLineArgumentsSnapshot)>>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task Initialize_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            var context = IWorkspaceProjectContextMockFactory.Create();
            Assert.Throws<ObjectDisposedException>(() =>
            {
                applyChangesToWorkspace.Initialize(context);
            });
        }

        [Fact]
        public async Task GetEvaluationRules_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                applyChangesToWorkspace.GetProjectEvaluationRules();
            });
        }

        [Fact]
        public async Task GetDesignTimeRules_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                applyChangesToWorkspace.GetProjectBuildRules();
            });
        }

        [Fact]
        public async Task ApplyProjectEvaluation_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            var update = Mock.Of<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, IProjectSubscriptionUpdate SourceItemsUpdate)>>();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ApplyProjectBuild_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            var update = Mock.Of<IProjectVersionedValue<(IProjectSubscriptionUpdate ProjectUpdate, CommandLineArgumentsSnapshot CommandLineArgumentsSnapshot)>>();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(), CancellationToken.None);
            });
        }

        [Fact]
        public void GetProjectEvaluationRules_ReturnsAllProjectEvaluationRuleNames()
        {
            var handler1 = IEvaluationHandlerFactory.ImplementProjectEvaluationRule("Rule1");
            var handler2 = IEvaluationHandlerFactory.ImplementProjectEvaluationRule("Rule2");

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler1, handler2 });

            var result = applyChangesToWorkspace.GetProjectEvaluationRules();

            Assert.Equal(new[] { "Rule1", "Rule2" }, result.OrderBy(n => n));
        }

        [Fact]
        public void GetDesignTimeRules_ReturnsCompilerCommandLineArgs()
        {
            var applyChangesToWorkspace = CreateInitializedInstance();

            var result = applyChangesToWorkspace.GetProjectBuildRules();

            Assert.Single(result, "CompilerCommandLineArgs");
        }

        [Fact]
        public void ApplyProjectEvaluation_WhenNoRuleChanges_DoesNotCallHandler()
        {
            int callCount = 0;
            var handler = IEvaluationHandlerFactory.ImplementHandle("RuleName", (context, projectConfiguration, version, description, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "RuleName": {
                            "Difference": { 
                                "AnyChanges": false
                            },
                        }
                    }
                }
                """);

            var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "RuleName": {
                            "Difference": { 
                                "AnyChanges": false
                            },
                        }
                    }
                }
                """);

            var update = IProjectVersionedValueFactory.Create((projectUpdate, sourceItemsUpdate), ProjectDataSources.ConfiguredProjectVersion, 1);

            applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(true, false), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void ApplyProjectBuild_WhenNoCompilerCommandLineArgsRuleChanges_Throws()
        {
            int callCount = 0;
            var handler = ICommandLineHandlerFactory.ImplementHandle((context, version, added, removed, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "CompilerCommandLineArgs": {
                            "Difference": { 
                                "AnyChanges": false
                            },
                        }
                    }
                }
                """);

            var update = IProjectVersionedValueFactory.Create((projectUpdate, _unchangedCommandLineArguments));

            Assert.ThrowsAny<Exception>(() => applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(), CancellationToken.None));

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void ApplyProjectEvaluation_ProjectUpdate_CallsHandler()
        {
            (IComparable version, IProjectChangeDescription description, ContextState state, IProjectDiagnosticOutputService logger) result = default;

            var handler = IEvaluationHandlerFactory.ImplementHandle("RuleName", (context, projectConfiguration, version, description, state, logger) =>
            {
                result = (version, description, state, logger);
            });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "RuleName": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        }
                    }
                }
                """);

            var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();
            int version = 2;

            var update = IProjectVersionedValueFactory.Create((projectUpdate, sourceItemsUpdate), ProjectDataSources.ConfiguredProjectVersion, version);

            applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: true), CancellationToken.None);

            Assert.Equal(version, result.version);
            Assert.NotNull(result.description);
            Assert.True(result.state.IsActiveEditorContext);
            Assert.True(result.state.IsActiveConfiguration);
            Assert.NotNull(result.logger);
        }

        [Fact]
        public void ApplyProjectEvaluation_SourceItems_CallsHandler()
        {
            (IComparable version, IImmutableDictionary<string, IProjectChangeDescription> description, ContextState state, IProjectDiagnosticOutputService logger) result = default;

            var handler = ISourceItemsHandlerFactory.ImplementHandle((context, version, description, state, logger) =>
            {
                result = (version, description, state, logger);
            });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();
            var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "RuleName": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        }
                    }
                }
                """);
            int version = 2;

            var update = IProjectVersionedValueFactory.Create((projectUpdate, sourceItemsUpdate), ProjectDataSources.ConfiguredProjectVersion, version);

            applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: true), CancellationToken.None);

            Assert.Equal(version, result.version);
            Assert.NotNull(result.description);
            Assert.True(result.state.IsActiveEditorContext);
            Assert.True(result.state.IsActiveConfiguration);
            Assert.NotNull(result.logger);
        }

        [Fact]
        public void ApplyProjectBuild_ParseCommandLineAndCallsHandler()
        {
            (IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IProjectDiagnosticOutputService logger) result = default;

            var handler = ICommandLineHandlerFactory.ImplementHandle((context, version, added, removed, state, logger) =>
            {
                result = (version, added, removed, state, logger);
            });

            var parser = ICommandLineParserServiceFactory.CreateCSharp();

            var applyChangesToWorkspace = CreateInitializedInstance(commandLineParser: parser, handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
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
                """);
            int version = 2;

            var update = IProjectVersionedValueFactory.Create((projectUpdate, _changedCommandLineArguments), ProjectDataSources.ConfiguredProjectVersion, version);

            applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), CancellationToken.None);

            Assert.Equal(version, result.version);
            Assert.True(result.state.IsActiveEditorContext);
            Assert.NotNull(result.logger);

            Assert.Single(result.added!.MetadataReferences.Select(r => r.Reference), "Added.dll");
            Assert.Single(result.removed!.MetadataReferences.Select(r => r.Reference), "Removed.dll");
        }

        [Fact]
        public void ApplyProjectEvaluation_WhenCancellationTokenCancelled_StopsProcessingHandlersAndThrowOperationCanceled()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            int callCount = 0;

            var handler1 = IEvaluationHandlerFactory.ImplementHandle("RuleName1", delegate { cancellationTokenSource.Cancel(); });
            var handler2 = IEvaluationHandlerFactory.ImplementHandle("RuleName2", delegate { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler1, handler2 });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "RuleName1": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        },
                        "RuleName2": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        }
                    }
                }
                """);

            var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();

            var update = IProjectVersionedValueFactory.Create((projectUpdate, sourceItemsUpdate), ProjectDataSources.ConfiguredProjectVersion, 1);

            Assert.Throws<OperationCanceledException>(() =>
            {
                applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), cancellationTokenSource.Token);
            });

            Assert.True(cancellationTokenSource.IsCancellationRequested);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public void ApplyProjectBuild_WhenCancellationTokenCancelled_StopsProcessingHandlersAndThrowOperationCanceled()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            int callCount = 0;

            var handler1 = ICommandLineHandlerFactory.ImplementHandle(delegate { cancellationTokenSource.Cancel(); });
            var handler2 = ICommandLineHandlerFactory.ImplementHandle(delegate { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler1, handler2 });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "CompilerCommandLineArgs": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        }
                    }
                }
                """);
            var update = IProjectVersionedValueFactory.Create((projectUpdate, _changedCommandLineArguments), ProjectDataSources.ConfiguredProjectVersion, 1);

            Assert.Throws<OperationCanceledException>(() =>
            {
                applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), cancellationTokenSource.Token);
            });

            Assert.True(cancellationTokenSource.IsCancellationRequested);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public void ApplyProjectEvaluation_IgnoresCommandLineHandlers()
        {
            int callCount = 0;
            var handler = ICommandLineHandlerFactory.ImplementHandle((context, version, added, removed, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "CompilerCommandLineArgs": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        },
                        "RuleName": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        }
                    }
                }
                """);

            var sourceItemsUpdate = IProjectSubscriptionUpdateFactory.CreateEmpty();

            var update = IProjectVersionedValueFactory.Create((projectUpdate, sourceItemsUpdate), ProjectDataSources.ConfiguredProjectVersion, 1);

            applyChangesToWorkspace.ApplyProjectEvaluation(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void ApplyProjectBuild_IgnoresEvaluationHandlers()
        {
            int callCount = 0;
            var handler = IEvaluationHandlerFactory.ImplementHandle("RuleName", (context, projectConfiguration, version, description, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
                """
                {
                   "ProjectChanges": {
                        "CompilerCommandLineArgs": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        },
                        "RuleName": {
                            "Difference": { 
                                "AnyChanges": true
                            },
                        }
                    }
                }
                """);
            var update = IProjectVersionedValueFactory.Create((projectUpdate, _changedCommandLineArguments), ProjectDataSources.ConfiguredProjectVersion, 1);

            applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void ApplyProjectBuild_WhenDesignTimeBuildFails_SetsLastDesignTimeBuildSucceededToFalse()
        {
            var applyChangesToWorkspace = CreateInitializedInstance(out var context);
            context.LastDesignTimeBuildSucceeded = true;

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
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
                """);
            var update = IProjectVersionedValueFactory.Create((projectUpdate, _changedCommandLineArguments), ProjectDataSources.ConfiguredProjectVersion, 1);

            applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(), CancellationToken.None);

            Assert.False(context.LastDesignTimeBuildSucceeded);
        }

        [Fact]
        public void ApplyProjectBuild_AfterFailingDesignTimeBuildSucceeds_SetsLastDesignTimeBuildSucceededToTrue()
        {
            var applyChangesToWorkspace = CreateInitializedInstance(out var context);

            var projectUpdate = IProjectSubscriptionUpdateFactory.FromJson(
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
                """);
            var update = IProjectVersionedValueFactory.Create((projectUpdate, _changedCommandLineArguments), ProjectDataSources.ConfiguredProjectVersion, 1);

            applyChangesToWorkspace.ApplyProjectBuild(update, new ContextState(true, false), CancellationToken.None);

            Assert.True(context.LastDesignTimeBuildSucceeded);
        }

        private static ApplyChangesToWorkspaceContext CreateInitializedInstance(ConfiguredProject? project = null, ICommandLineParserService? commandLineParser = null, IProjectDiagnosticOutputService? logger = null, params IWorkspaceContextHandler[] handlers)
        {
            return CreateInitializedInstance(out _, project, commandLineParser, logger, handlers);
        }

        private static ApplyChangesToWorkspaceContext CreateInitializedInstance(out IWorkspaceProjectContext context, ConfiguredProject? project = null, ICommandLineParserService? commandLineParser = null, IProjectDiagnosticOutputService? logger = null, params IWorkspaceContextHandler[] handlers)
        {
            var applyChangesToWorkspace = CreateInstance(project, commandLineParser, logger, handlers);
            context = IWorkspaceProjectContextMockFactory.Create();

            applyChangesToWorkspace.Initialize(context);

            return applyChangesToWorkspace;
        }

        private static ApplyChangesToWorkspaceContext CreateInstance(ConfiguredProject? project = null, ICommandLineParserService? commandLineParser = null, IProjectDiagnosticOutputService? logger = null, params IWorkspaceContextHandler[] handlers)
        {
            if (project == null)
            {
                var unconfiguredProject = UnconfiguredProjectFactory.ImplementFullPath(@"C:\Project\Project.csproj");

                project = ConfiguredProjectFactory.ImplementUnconfiguredProject(unconfiguredProject);
            }

            commandLineParser ??= ICommandLineParserServiceFactory.Create();
            logger ??= IProjectDiagnosticOutputServiceFactory.Create();

            var factories = handlers.Select(h => ExportFactoryFactory.ImplementCreateValueWithAutoDispose(() => h))
                                    .ToArray();

            var applyChangesToWorkspaceContext = new ApplyChangesToWorkspaceContext(project, logger, factories);

            applyChangesToWorkspaceContext.CommandLineParsers.Add(commandLineParser);

            return applyChangesToWorkspaceContext;
        }
    }
}
