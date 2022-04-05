// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Test.Apex.VisualStudio.Search;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [TestClass]
    public sealed class ProjectFileEditorTests : IntegrationTestBase
    {
        [TestMethod]
        public void ProjectFileEditor_DoubleClick_OpensEditor()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Open project file editor"))
            {
                consoleProject.DoubleClick();
            }

            using (Scope.Enter("Verify Editor"))
            {
                var editor = VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor;
                editor.WaitForFullyLoaded();
                editor.Verify.FilePathIs(consoleProject.FullPath);
            }
        }

        [TestMethod]
        public void ProjectFileEditor_EditsMade_TabMarkedDirty()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Open project file editor"))
            {
                consoleProject.DoubleClick();
            }

            using (Scope.Enter("Verify Editor"))
            {
                var editor = VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor;
                editor.WaitForFullyLoaded();
                editor.Editor.Caret.MoveDown(3);
                editor.Editor.Edit.InsertLineBelow(2);
                editor.Verify.IsDirty();
            }
        }

        [TestMethod]
        public void ProjectFileEditor_ProjectPropertyChanged_TabOpensDirty()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Change project property"))
            {
                consoleProject.Properties.SetValue(ProjectProperty.OutputType, prjOutputType.prjOutputTypeLibrary);
            }

            using (Scope.Enter("Open project file editor"))
            {
                consoleProject.DoubleClick();
            }

            using (Scope.Enter("Verify Editor"))
            {
                var editor = VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor;
                editor.WaitForFullyLoaded();
                editor.Verify.IsDirty();
            }
        }

        [TestMethod]
        public void ProjectFileEditor_ProjectPropertyChanged_EditorShowsProjectChanges()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Change project property"))
            {
                consoleProject.Properties.SetValue(ProjectProperty.OutputType, prjOutputType.prjOutputTypeLibrary);
            }

            using (Scope.Enter("Open project file editor"))
            {
                consoleProject.DoubleClick();
            }

            using (Scope.Enter("Verify Text Buffer Changed"))
            {
                var editor = VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor;
                editor.WaitForFullyLoaded();
                editor.Editor.Verify.ContainsText("<OutputType>Library</OutputType>");
            }
        }

        [TestMethod]
        public void ProjectFileEditor_ReplaceAllWhileEditorLoaded_ReplacementsNotLoaded()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Open project file editor"))
            {
                consoleProject.DoubleClick();
            }

            using (Scope.Enter("Change project property"))
            {
                VisualStudio.ObjectModel.Search.FindReplace.ReplaceAllInFiles("Exe", "Library", LookInFilesSearchScopes.EntireSolution);
            }

            using (Scope.Enter("Verify Text Buffer Changed"))
            {
                var editor = VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor;
                editor.Editor.Verify.ContainsText("<OutputType>Library</OutputType>");
            }

            using (Scope.Enter("Verify Project Unchanged"))
            {
                consoleProject.Properties.Verify.PropertyValueIs(ProjectProperty.OutputType, prjOutputType.prjOutputTypeExe);
            }
        }

        [TestMethod]
        public void ProjectFileEditor_ReplaceAllWhileEditorNotLoaded_ReplacementsLoaded()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Change project property"))
            {
                VisualStudio.ObjectModel.Search.FindReplace.ReplaceAllInFiles("Exe", "Library", LookInFilesSearchScopes.EntireSolution, keepModifiedFilesOpen: KeepModifiedFilesOpen.No).WaitForReplaceInFilesCompletion();
            }

            using (Scope.Enter("Open project file editor"))
            {
                consoleProject.DoubleClick();
            }

            using (Scope.Enter("Verify Text Buffer Changed"))
            {
                var editor = VisualStudio.ObjectModel.WindowManager.ActiveDocumentWindowAsTextEditor;
                editor.Editor.Verify.ContainsText("<OutputType>Library</OutputType>");
            }

            using (Scope.Enter("Verify Project Changed"))
            {
                // project reload is on a one second delay
                Services.Synchronization.WaitFor(TimeSpan.FromSeconds(1), () => consoleProject.Properties.Verify.PropertyValueIs(ProjectProperty.OutputType, prjOutputType.prjOutputTypeLibrary));
            }
        }

        [TestMethod]
        [Ignore("The new find results pain is not exposed via Apex correctly so this test ends up timing out, though you can visually inspect the results while you're waiting if you like")]
        public void ProjectFileEditor_FindAllAfterPropertyChange_FindsProjectFile()
        {
            var solution = VisualStudio.ObjectModel.Solution;

            ProjectTestExtension consoleProject;

            using (Scope.Enter("Create Project"))
            {
                consoleProject = solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                solution.Verify.HasProject();
            }

            using (Scope.Enter("Change project property"))
            {
                consoleProject.Properties.SetValue(ProjectProperty.OutputType, prjOutputType.prjOutputTypeLibrary);
            }

            using (Scope.Enter("Find changed property"))
            {
                var find = VisualStudio.ObjectModel.Search.FindReplace.FindAll("Library", LookInFilesSearchScopes.EntireSolution);
                find.WaitForReplaceInFilesCompletion();
                Assert.AreEqual(find.ReplacedItemCount, 1);
            }
        }
    }
}
