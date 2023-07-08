// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [TestClass]
    public sealed class CreateProjectTests : IntegrationTestBase
    {
        [TestMethod]
        public void CreateProject_CreateAndBuild()
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

            using (Scope.Enter("Wait for IntelliSense"))
            {
                solution.WaitForIntellisenseStage();
            }

            using (Scope.Enter("Verify dependency nodes"))
            {
                var dependencies = solution.SolutionExplorer.FindItemRecursive("Dependencies", expandToFind: true);
                dependencies.Select();
                dependencies.ExpandAll();
                Assert.AreEqual("Dependencies", dependencies.Name);
                var frameworks = dependencies.Items.FirstOrDefault();
                Assert.IsNotNull(frameworks);
                Assert.AreEqual("Frameworks", frameworks.Name);
            }

            using (Scope.Enter("Build Project"))
            {
                solution.BuildManager.Build();
                solution.BuildManager.WaitForBuildFinished();
                var success = solution.BuildManager.Verify.HasFinished();
                Assert.IsTrue(success, $"project '{consoleProject.FileName}' failed to finish building.");
            }

            using (Scope.Enter("Verify Build Succeeded"))
            {
                var success =
                    solution.BuildManager.Verify.ProjectBuilt(consoleProject) &&
                    solution.BuildManager.Verify.Succeeded();

                if (!success)
                {
                    VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.WaitForErrorListItems();
                    var errors = VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.Errors.Select(x => $"Description:'{x.Description}' Project:{x.ProjectName} Line:'{x.LineNumber}'").ToArray();
                    Assert.Fail($"project '{consoleProject.FileName}' failed to build.{Environment.NewLine}errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
                }
            }
        }
    }
}
