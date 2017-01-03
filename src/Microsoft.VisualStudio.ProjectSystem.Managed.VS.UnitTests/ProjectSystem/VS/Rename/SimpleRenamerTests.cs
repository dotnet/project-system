// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [ProjectSystemTrait]
    public class SimpleRenamerTests
    {
        [Theory]
        [InlineData("class Foo{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo {} partial class Foo {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo { decimal price; string title; string author;}", "Foo.cs", "Bar.cs")]
        [InlineData("enum Foo { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        [InlineData("namespace n1 {class Foo{}} namespace n2 {class Foo{}}", "Foo.cs", "Bar.cs")]
        public async Task Rename_Symbol_Should_TriggerUserConfirmationAsync(string soureCode, string oldFilePath, string newFilePath)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Create();

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.CSharp);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Once);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("class Foo{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo {} partial class Foo {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo { decimal price; string title; string author;}", "Foo.cs", "Bar.cs")]
        [InlineData("enum Foo { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        [InlineData("namespace n1 {class Foo{}} namespace n2 {class Foo{}}", "Foo.cs", "Bar.cs")]
        [InlineData("namespace n1 {class Foo{}} namespace n2 {class Foo{}}", "Foo.cs", "Bar.cs")]
        [InlineData("class Strasse{}", "Strasse.cs", "Straße.cs")]
        [InlineData("class Foo{}", "Foo.cs", "foo.cs")]
        [InlineData("class Foo{}", "Foo.cs", "Folder1\\foo.cs")]
        public async Task Rename_Symbol_Should_HappenAsync(string soureCode, string oldFilePath, string newFilePath)
        {

            var userNotificationServices = IUserNotificationServicesFactory.Implement(true);
            var roslynServices = IRoslynServicesFactory.Implement(DummySolution(), true);

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.CSharp);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Once);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Once);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Once);
        }


        [Theory]
        [InlineData("class Foo{}", "Bar.cs", "Foo.cs")]
        [InlineData("class Foo1{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo1 { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo1(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo1 {} partial class Foo2 {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo1 { decimal price; string title; string author;}", "Foo.cs", "Bar.cs")]
        [InlineData("enum Foo1 { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        [InlineData("class Foo{}", "Bar.cs", "Foo`.cs")]
        [InlineData("class Foo{}", "Bar.cs", "Foo@.cs")]
        [InlineData("class Foo{}", "Foo.cs", "Foo.cs")]
        [InlineData("class Foo{}", "Foo.cs", "Folder1\\Foo.cs")]
        public async Task Rename_Symbol_Should_Not_HappenAsync(string soureCode, string oldFilePath, string newFilePath)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Create();

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.CSharp);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        [Theory]
        [InlineData("Class Foo End Class", "Foo.vb", "foo.vb")]
        [InlineData("Class Foo End Class", "Foo.vb", "Folder1\\foo.vb")]
        public async Task Rename_Symbol_Should_Not_HappenAsync_ForVB(string soureCode, string oldFilePath, string newFilePath)
        {

            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Create();

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.VisualBasic);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        private async Task RenameAsync(string soureCode, string oldFilePath, string newFilePath, IUserNotificationServices userNotificationServices, IRoslynServices roslynServices, string language)
        {
            using (var ws = new AdhocWorkspace())
            {
                var projectId = ProjectId.CreateNewId();
                Solution solution = ws.AddSolution(InitializeWorkspace(projectId, newFilePath, soureCode,language));
                Project project = (from d in solution.Projects where d.Id == projectId select d).FirstOrDefault();

                var optionsSettingsFactory = IOptionsSettingsFactory.Implement((string category, string page, string property, bool defaultValue) => { return true; });

                var renamer = new Renamer(ws, IProjectThreadingServiceFactory.Create(), userNotificationServices, optionsSettingsFactory, roslynServices, project, oldFilePath, newFilePath);
                await renamer.RenameAsync(project);
            }
        }

        private Solution DummySolution()
        {
            using (var ws = new AdhocWorkspace())
            {
                return ws.AddSolution(InitializeWorkspace(ProjectId.CreateNewId(), "temp.cs", "class temp{}", LanguageNames.CSharp));
            }
        }

        private SolutionInfo InitializeWorkspace(ProjectId projectId, string fileName, string code, string language)
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
                        filePath: @"C:\project1.csproj",
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
    }
}
