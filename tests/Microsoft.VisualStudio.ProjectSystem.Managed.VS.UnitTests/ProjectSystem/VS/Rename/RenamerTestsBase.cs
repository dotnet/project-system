// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

//using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ProjectSystem.Refactor;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;
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

        internal async Task RenameAsync(string sourceCode, string oldFilePath, string newFilePath, IUserNotificationServices userNotificationServices, IRoslynServices roslynServices, IVsOnlineServices vsOnlineServices, string language)
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
            var projectThreadingService = (new Mock<IProjectThreadingService>().Object);
            var extensibility = (new Mock<IVsUIService<IVsExtensibility, IVsExtensibility3>>().Object);
            var dte = IVsUIServiceFactory.Create<Shell.Interop.SDTE, EnvDTE.DTE>(null!);

            //var renamer = new RenamerProjectTreeActionHandler(unconfiguredProject, 
            //                                                  projectServices, 
            //                                                  ws, 
            //                                                  environmentOptionsFactory, 
            //                                                  userNotificationServices, 
            //                                                  roslynServices, 
            //                                                  waitIndicator, 
            //                                                  vsOnlineServices, 
            //                                                  projectThreadingService,
            //                                                  extensibility);

//            var context = (new Mock<IProjectTreeActionHandlerContext>().Object);
//            var node = (new Mock<IProjectTree>().Object);
//            var value = "NewFilename.cs";
//            await renamer.RenameAsync(context, node, value);
//                         .TimeoutAfter(TimeSpan.FromSeconds(1));
        }
    }
}
