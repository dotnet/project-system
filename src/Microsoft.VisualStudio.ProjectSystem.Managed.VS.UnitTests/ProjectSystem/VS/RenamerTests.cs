// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class RenamerTests
    {
        bool PromptForRenameWasCalled { get; set; }

        [Theory]
        [InlineData("class Foo{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo {} partial class Foo {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo { decimal price; string title; string author;}", "Foo.cs", "Bar.cs" )]
        [InlineData("enum Foo { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        public void Rename_Symbol_Should_Happen(string soureCode, string oldFilePath, string newFilePath)
        {
            Rename(soureCode, oldFilePath, newFilePath);
            Assert.True(PromptForRenameWasCalled);
        }

        [Theory]
        [InlineData("class Foo{}", "Bar.cs", "Foo.cs")]
        [InlineData("class Foo1{}", "Foo.cs", "Bar.cs")]
        [InlineData("interface Foo1 { void m1(); void m2();}", "Foo.cs", "Bar.cs")]
        [InlineData("delegate int Foo1(string s);", "Foo.cs", "Bar.cs")]
        [InlineData("partial class Foo1 {} partial class Foo2 {}", "Foo.cs", "Bar.cs")]
        [InlineData("struct Foo1 { decimal price; string title; string author;}", "Foo.cs", "Bar.cs")]
        [InlineData("enum Foo1 { None, enum1, enum2, enum3, enum4 };", "Foo.cs", "Bar.cs")]
        public void Rename_Symbol_Should_Not_Happen(string soureCode, string oldFilePath, string newFilePath)
        {
            Rename(soureCode, oldFilePath, newFilePath);
            Assert.False(PromptForRenameWasCalled);
        }

        private void Rename(string soureCode, string oldFilePath, string newFilePath)
        {
            using (var ws = new AdhocWorkspace())
            {
                var projectId = ProjectId.CreateNewId();
                Solution solution = ws.AddSolution(InitializeWorkspace(projectId, newFilePath, soureCode));
                Project project = (from d in solution.Projects where d.Id == projectId select d).FirstOrDefault();

                var threadingService = IProjectThreadingServiceFactory.Create();
                var vsEnvironmentServices = IVsEnvironmentServicesFactory.Implement(f => PromptForRenameAsync(newFilePath));

                var renamer = new Renamer(ws, threadingService, vsEnvironmentServices, project, oldFilePath, newFilePath);
                renamer.Rename(project);
            }
        }

        private async Task<bool> PromptForRenameAsync(string newName)
        {
            PromptForRenameWasCalled = true;
            return await Task.FromResult(false);
        }
        
        private SolutionInfo InitializeWorkspace(ProjectId projectId, string fileName, string code)
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
                        language: LanguageNames.CSharp,
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
