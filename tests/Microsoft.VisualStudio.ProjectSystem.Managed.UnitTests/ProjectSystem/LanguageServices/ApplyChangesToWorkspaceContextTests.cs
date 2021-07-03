// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class ApplyChangesToWorkspaceContextTests
    {
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
        public void Initialize_InitializesHandlers()
        {
            IWorkspaceProjectContext? result = null;
            var handler = IWorkspaceContextHandlerFactory.ImplementInitialize((c) => { result = c; });

            var applyChangesToWorkspace = CreateInstance(handlers: handler);
            var context = IWorkspaceProjectContextMockFactory.Create();

            applyChangesToWorkspace.Initialize(context);

            Assert.Same(context, result);
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
        public async Task ApplyProjectEvaluationAsync_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();

            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return applyChangesToWorkspace.ApplyProjectEvaluationAsync(update, new ContextState(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_WhenNotInitialized_ThrowsInvalidOperation()
        {
            var applyChangesToWorkspace = CreateInstance();

            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
            var buildSnapshot = IProjectBuildSnapshotFactory.Create();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot,new ContextState(), CancellationToken.None);
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
        public async Task ApplyProjectEvaluationAsync_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            {
                return applyChangesToWorkspace.ApplyProjectEvaluationAsync(update, new ContextState(), CancellationToken.None);
            });
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_WhenDisposed_ThrowsObjectDisposed()
        {
            var applyChangesToWorkspace = CreateInstance();

            await applyChangesToWorkspace.DisposeAsync();

            var update = Mock.Of<IProjectVersionedValue<IProjectSubscriptionUpdate>>();
            var buildSnapshot = IProjectBuildSnapshotFactory.Create();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            {
                return applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(), CancellationToken.None);
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
        public async Task ApplyProjectEvaluationAsync_WhenNoRuleChanges_DoesNotCallHandler()
        {
            int callCount = 0;
            var handler = IEvaluationHandlerFactory.ImplementHandle("RuleName", (version, description, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""RuleName"": {
            ""Difference"": { 
                ""AnyChanges"": false
            },
        }
    }
}");

            await applyChangesToWorkspace.ApplyProjectEvaluationAsync(update, new ContextState(true, false), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_WhenNoCompilerCommandLineArgsRuleChanges_DoesNotCallHandler()
        {
            int callCount = 0;
            var handler = ICommandLineHandlerFactory.ImplementHandle((version, added, removed, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var buildSnapshot = IProjectBuildSnapshotFactory.Create();
            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": false
            },
        }
    }
}");

            await applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task ApplyProjectEvaluationAsync_CallsHandler()
        {
            (IComparable version, IProjectChangeDescription description, ContextState state, IProjectDiagnosticOutputService logger) result = default;

            var handler = IEvaluationHandlerFactory.ImplementHandle("RuleName", (version, description, state, logger) =>
            {
                result = (version, description, state, logger);
            });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var update = IProjectVersionedValueFactory.FromJson(version: 2,
@"{
   ""ProjectChanges"": {
        ""RuleName"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        }
    }
}");
            await applyChangesToWorkspace.ApplyProjectEvaluationAsync(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: true), CancellationToken.None);

            Assert.Equal(2, result.version);
            Assert.NotNull(result.description);
            Assert.True(result.state.IsActiveEditorContext);
            Assert.True(result.state.IsActiveConfiguration);
            Assert.NotNull(result.logger);
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_ParseCommandLineAndCallsHandler()
        {
            (IComparable version, BuildOptions added, BuildOptions removed, ContextState state, IProjectDiagnosticOutputService logger) result = default;

            var handler = ICommandLineHandlerFactory.ImplementHandle((version, added, removed, state, logger) =>
            {
                result = (version, added, removed, state, logger);
            });

            var parser = ICommandLineParserServiceFactory.CreateCSharp();

            var applyChangesToWorkspace = CreateInitializedInstance(commandLineParser: parser, handlers: new[] { handler });

            var buildSnapshot = IProjectBuildSnapshotFactory.Create();
            var update = IProjectVersionedValueFactory.FromJson(version: 2,
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": true,
                ""AddedItems"" : [ ""/reference:Added.dll"" ],
                ""RemovedItems"" : [ ""/reference:Removed.dll"" ]
            }
        }
    }
}");
            await applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), CancellationToken.None);

            Assert.Equal(2, result.version);
            Assert.True(result.state.IsActiveEditorContext);
            Assert.NotNull(result.logger);

            Assert.Single(result.added!.MetadataReferences.Select(r => r.Reference), "Added.dll");
            Assert.Single(result.removed!.MetadataReferences.Select(r => r.Reference), "Removed.dll");
        }

        [Fact]
        public async Task ApplyProjectEvaluationAsync_WhenCancellationTokenCancelled_StopsProcessingHandlersAndThrowOperationCanceled()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var handler1 = IEvaluationHandlerFactory.ImplementHandle("RuleName1", (version, description, state, logger) =>
            {
                cancellationTokenSource.Cancel();
            });

            int callCount = 0;
            var handler2 = IEvaluationHandlerFactory.ImplementHandle("RuleName2", (version, description, state, logger) =>
            {
                callCount++;
            });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler1, handler2 });

            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""RuleName1"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        },
        ""RuleName2"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        }
    }
}");
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return applyChangesToWorkspace.ApplyProjectEvaluationAsync(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), cancellationTokenSource.Token);
            });

            Assert.True(cancellationTokenSource.IsCancellationRequested);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_WhenCancellationTokenCancelled_StopsProcessingHandlersAndThrowOperationCanceled()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var handler1 = ICommandLineHandlerFactory.ImplementHandle((version, added, removed, state, logger) =>
            {
                cancellationTokenSource.Cancel();
            });

            int callCount = 0;
            var handler2 = ICommandLineHandlerFactory.ImplementHandle((version, added, removed, state, logger) =>
            {
                callCount++;
            });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler1, handler2 });

            var buildSnapshot = IProjectBuildSnapshotFactory.Create();
            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        }
    }
}");
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), cancellationTokenSource.Token);
            });

            Assert.True(cancellationTokenSource.IsCancellationRequested);
            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task ApplyProjectEvaluationAsync_IgnoresCommandLineHandlers()
        {
            int callCount = 0;
            var handler = ICommandLineHandlerFactory.ImplementHandle((version, added, removed, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        },
        ""RuleName"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        }
    }
}");
            await applyChangesToWorkspace.ApplyProjectEvaluationAsync(update, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_IgnoresEvaluationHandlers()
        {
            int callCount = 0;
            var handler = IEvaluationHandlerFactory.ImplementHandle("RuleName", (version, description, state, logger) => { callCount++; });

            var applyChangesToWorkspace = CreateInitializedInstance(handlers: new[] { handler });

            var buildSnapshot = IProjectBuildSnapshotFactory.Create();
            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        },
        ""RuleName"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
        }
    }
}");
            await applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(isActiveEditorContext: true, isActiveConfiguration: false), CancellationToken.None);

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_WhenDesignTimeBuildFails_SetsLastDesignTimeBuildSucceededToFalse()
        {
            var applyChangesToWorkspace = CreateInitializedInstance(out var context);
            context.LastDesignTimeBuildSucceeded = true;

            var buildSnapshot = IProjectBuildSnapshotFactory.Create();

            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
            ""After"": { 
                ""EvaluationSucceeded"": false
            }
        }
    }
}");

            await applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(), CancellationToken.None);

            Assert.False(context.LastDesignTimeBuildSucceeded);
        }

        [Fact]
        public async Task ApplyProjectBuildAsync_AfterFailingDesignTimeBuildSucceeds_SetsLastDesignTimeBuildSucceededToTrue()
        {
            var applyChangesToWorkspace = CreateInitializedInstance(out var context);

            var buildSnapshot = IProjectBuildSnapshotFactory.Create();
            var update = IProjectVersionedValueFactory.FromJson(
@"{
   ""ProjectChanges"": {
        ""CompilerCommandLineArgs"": {
            ""Difference"": { 
                ""AnyChanges"": true
            },
            ""After"": { 
                ""EvaluationSucceeded"": true
            }
        }
    }
}");

            await applyChangesToWorkspace.ApplyProjectBuildAsync(update, buildSnapshot, new ContextState(true, false), CancellationToken.None);

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
