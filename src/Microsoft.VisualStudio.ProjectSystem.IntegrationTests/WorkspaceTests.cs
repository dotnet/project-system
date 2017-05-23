// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class WorkspaceTests : AbstractIntegrationTest
    {
        protected override string DefaultLanuageName => LanguageNames.CSharp;

        public WorkspaceTests(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpErrorListTests), WellKnownProjectTemplates.CSharpNetCoreClassLibrary, instanceFactory)
        {
            VisualStudio.SolutionExplorer.OpenFile(Project, "Class1.cs");
        }

        [Fact(Skip ="Classification doesn't work when files are loaded in misc workspace"), Trait("Integration", "Workspace")]
        public void OpenCSharpThenVBSolution()
        {
            VisualStudio.Editor.SetText(@"using System; class Program { Exception e; }");
            VisualStudio.Editor.PlaceCaret("Exception");
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
            VisualStudio.SolutionExplorer.CloseSolution();
            VisualStudio.SolutionExplorer.CreateSolution(nameof(WorkspaceTests));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary, languageName: LanguageNames.VisualBasic);
            VisualStudio.SolutionExplorer.OpenFile(testProj, "Class1.vb");
            VisualStudio.Editor.SetText(@"Imports System
Class Program
    Private e As Exception
End Class");
            var path = VisualStudio.SolutionExplorer.SolutionFileFullPath;
            VisualStudio.Editor.PlaceCaret("Exception");
            VisualStudio.WaitForApplicationIdle();
            VisualStudio.Editor.Verify.CurrentTokenType(tokenType: "class name");
        }

        [Fact(Skip = "Unload/Reload does not work"), Trait("Integration", "Workspace")]
        public void MetadataReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.EditProjectFile(project);
            VisualStudio.Editor.SetText(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>");
            VisualStudio.SolutionExplorer.SaveAll();
            VisualStudio.SolutionExplorer.UnloadProject(project);
            VisualStudio.SolutionExplorer.ReloadProject(project);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            var windowsBase = new ProjectUtils.AssemblyReference("WindowsBase");
            VisualStudio.SolutionExplorer.AddMetadataReference(windowsBase, project);
            VisualStudio.Editor.SetText("class C { System.Windows.Point p; }");
            VisualStudio.Editor.PlaceCaret("Point");
            VisualStudio.Editor.Verify.CurrentTokenType("struct name");
            VisualStudio.SolutionExplorer.RemoveMetadataReference(windowsBase, project);
            VisualStudio.Editor.Verify.CurrentTokenType("identifier");
        }

        [Fact, Trait("Integration", "Workspace")]
        public void ProjectReference()
        {
            var project = new ProjectUtils.Project(ProjectName);
            var csProj2 = new ProjectUtils.Project("CSProj2");
            VisualStudio.SolutionExplorer.AddProject(csProj2, projectTemplate: WellKnownProjectTemplates.CSharpNetCoreClassLibrary, languageName: LanguageNames.CSharp);
            var projectName = new ProjectUtils.ProjectReference(ProjectName);
            VisualStudio.SolutionExplorer.AddProjectReference(fromProjectName: csProj2, toProjectName: projectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Program.cs", open: true, contents: "public class Class1 { }");
            VisualStudio.SolutionExplorer.AddFile(csProj2, "Program.cs", open: true, contents: "public class Class2 { Class1 c; }");
            VisualStudio.SolutionExplorer.OpenFile(csProj2, "Program.cs");
            VisualStudio.Editor.PlaceCaret("Class1");
            VisualStudio.Editor.Verify.CurrentTokenType("class name");
            VisualStudio.SolutionExplorer.RemoveProjectReference(projectReferenceName: projectName, projectName: csProj2);
            VisualStudio.Editor.Verify.CurrentTokenType("identifier");
        }

        [Fact(Skip = "Cannot set 'option infer' from DTE for CPS projects"), Trait("Integration", "Workspace")]
        public void ProjectProperties()
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(WorkspaceTests));
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddProject(project, WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary, LanguageNames.VisualBasic);
            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.vb");
            VisualStudio.Editor.SetText(@"Module Program
    Sub Main()
        Dim x = 42
        M(x)
    End Sub
    Sub M(p As Integer)
    End Sub
    Sub M(p As Object)
    End Sub
End Module");
            VisualStudio.Editor.PlaceCaret("(x)", charsOffset: -1);
            VisualStudio.Workspace.SetQuickInfo(true);
            VisualStudio.Workspace.SetOptionInfer(project.Name, true);
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Integer‎)‎ ‎(‎+‎ 1‎ overload‎)", VisualStudio.Editor.GetQuickInfo());
            VisualStudio.Workspace.SetOptionInfer(project.Name, false);
            VisualStudio.Editor.InvokeQuickInfo();
            Assert.Equal("Sub‎ Program.M‎(p‎ As‎ Object‎)‎ ‎(‎+‎ 1‎ overload‎)", VisualStudio.Editor.GetQuickInfo());
        }
    }
}
