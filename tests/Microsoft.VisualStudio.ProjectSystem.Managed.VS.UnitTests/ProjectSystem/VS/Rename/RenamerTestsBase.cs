﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

//using System;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.ProjectSystem.Refactor;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename;

public abstract class RenamerTestsBase
{
    protected abstract string ProjectFileExtension { get; }

    protected SolutionInfo InitializeWorkspace(ProjectId projectId, string fileName, string code, string language)
    {
        var solutionId = SolutionId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        return SolutionInfo.Create(
            solutionId,
            VersionStamp.Create(),
            projects: new ProjectInfo[]
            {
                ProjectInfo.Create(
                    id: projectId,
                    version: VersionStamp.Create(),
                    name: "Project1",
                    assemblyName: "Project1",
                    filePath: $@"C:\project1.{ProjectFileExtension}",
                    language: language,
                    documents: new DocumentInfo[]
                    {
                            DocumentInfo.Create(
                            documentId,
                            fileName,
                            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code),
                            VersionStamp.Create())),
                            filePath: fileName)
                     })
            });
    }

    internal class TestRenamerProjectTreeActionHandler : RenamerProjectTreeActionHandler
    {
        public TestRenamerProjectTreeActionHandler(
            UnconfiguredProject unconfiguredProject,
            IUnconfiguredProjectVsServices projectVsServices,
            [Import(typeof(VisualStudioWorkspace))]Lazy<Workspace> workspace,
            IEnvironmentOptions environmentOptions,
            IUserNotificationServices userNotificationServices,
            IRoslynServices roslynServices,
            IWaitIndicator waitService,
            IVsOnlineServices vsOnlineServices,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
            IProjectThreadingService threadingService,
            IVsUIService<IVsExtensibility, IVsExtensibility3> extensibility,
            IVsService<SVsOperationProgress, IVsOperationProgressStatusService> operationProgressService,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)
            : base(unconfiguredProject, projectVsServices, workspace, environmentOptions, userNotificationServices, roslynServices, waitService,
                   vsOnlineServices, projectAsynchronousTasksService, threadingService, extensibility, operationProgressService, settingsManagerService)
        {
        }

        protected override async Task CpsFileRenameAsync(IProjectTreeActionHandlerContext context, IProjectTree node, string value)
        {
            await Task.CompletedTask;
        }

        protected override async Task<bool> IsAutomationFunctionAsync()
        {
            return await Task.FromResult(false);
        }
    }

    internal async Task RenameAsync(string sourceCode, string oldFilePath, string newFilePath, 
        IUserNotificationServices userNotificationServices, 
        IRoslynServices roslynServices, 
        IVsOnlineServices vsOnlineServices, 
        string language, 
        IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)
    {
        // Configure mocks.
        var unconfiguredProject = UnconfiguredProjectFactory.Create(fullPath: $@"C:\project1.{ProjectFileExtension}");
        var projectServices = IUnconfiguredProjectVsServicesFactory.Implement(
            threadingServiceCreator: () => IProjectThreadingServiceFactory.Create(),
            unconfiguredProjectCreator: () => unconfiguredProject);

        using var ws = new AdhocWorkspace();
        ws.AddSolution(InitializeWorkspace(ProjectId.CreateNewId(), oldFilePath, sourceCode, language));

        var environmentOptionsFactory = IEnvironmentOptionsFactory.Implement<bool>((_, _, _, _) => true);
        var waitIndicator = Mock.Of<IWaitIndicator>();
        var projectAsynchronousTasksService = IProjectAsynchronousTasksServiceFactory.Create();
        var projectThreadingService = IProjectThreadingServiceFactory.Create();
        var refactorNotifyService = Mock.Of<IRefactorNotifyService>();
        var extensibility = Mock.Of<IVsUIService<IVsExtensibility, IVsExtensibility3>>();
        var operationProgressMock = Mock.Of<IVsService<SVsOperationProgress, IVsOperationProgressStatusService>>();
        var context = Mock.Of<IProjectTreeActionHandlerContext>();

        var mockNode = new Mock<IProjectTree>();
        mockNode.SetupGet(x => x.FilePath).Returns(oldFilePath);
        mockNode.SetupGet(x => x.IsFolder).Returns(false);
        var node = mockNode.Object;

        // Construct the renamer.
        var renamer = new TestRenamerProjectTreeActionHandler(
            unconfiguredProject,
            projectServices,
            new Lazy<Workspace>(() => ws),
            environmentOptionsFactory,
            userNotificationServices,
            roslynServices,
            waitIndicator,
            vsOnlineServices,
            projectAsynchronousTasksService,
            projectThreadingService,
            extensibility,
            operationProgressMock,
            settingsManagerService);

        // Perform the rename, with a timeout in case of hangs.
        await renamer
            .RenameAsync(context, node, newFilePath)
            .TimeoutAfter(TimeSpan.FromSeconds(30));
    }
}
