// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ProjectSystem.Refactor;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
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

        internal async Task RenameAsync(string sourceCode, string oldFilePath, string newFilePath, IUserNotificationServices userNotificationServices, IRoslynServices roslynServices, string language)
        {
            using var ws = new AdhocWorkspace();
            ws.AddSolution(InitializeWorkspace(ProjectId.CreateNewId(), newFilePath, sourceCode, language));

            var unconfiguredProject = UnconfiguredProjectFactory.Create(filePath: $@"C:\project1.{ProjectFileExtension}");
            var projectServices = IUnconfiguredProjectVsServicesFactory.Implement(
                    threadingServiceCreator: () => IProjectThreadingServiceFactory.Create(),
                    unconfiguredProjectCreator: () => unconfiguredProject);
            var unconfiguredProjectTasksService = IUnconfiguredProjectTasksServiceFactory.Create();
            var environmentOptionsFactory = IEnvironmentOptionsFactory.Implement((string category, string page, string property, bool defaultValue) => { return true; });
            var waitIndicator = (new Mock<IWaitIndicator>()).Object;
            var refactorNotifyService = (new Mock<IRefactorNotifyService>()).Object;

            var dte = IVsUIServiceFactory.Create<Shell.Interop.SDTE, EnvDTE.DTE>(null!);

            var renamer = new Renamer(projectServices, unconfiguredProjectTasksService, ws, dte, environmentOptionsFactory, userNotificationServices, roslynServices, waitIndicator, refactorNotifyService);
            await renamer.HandleRenameAsync(oldFilePath, newFilePath)
                         .TimeoutAfter(TimeSpan.FromSeconds(1));
        }
    }
}
