// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;

using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [TestClass]
    public class CreateProjectTests : TestBase
    {
        [TestMethod]
        public void CreateCSharpProject()
        {
            ProjectTestExtension consoleProject = default;
            using (Scope.Enter("Create Project"))
            {
                consoleProject = VisualStudio.ObjectModel.Solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                VisualStudio.ObjectModel.Solution.Verify.HasProject();
            }

            using (Scope.Enter("Wait for restore"))
            {
                Thread.Sleep(3 * 1000);
                var nuget = VisualStudio.Get<NuGetApexTestService>();
                nuget.WaitForAutoRestore();
            }

            using (Scope.Enter("Build Project"))
            {
                VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.ShowAllItems();
                VisualStudio.ObjectModel.Solution.BuildManager.Build();
                VisualStudio.ObjectModel.Solution.BuildManager.WaitForBuildFinished();
                var success = VisualStudio.ObjectModel.Solution.BuildManager.Verify.HasFinished();
                Assert.IsTrue(success, $"project '{consoleProject.FileName}' failed to finish building.");
            }

            using (Scope.Enter("Verify Build Succeeded"))
            {
                var success = VisualStudio.ObjectModel.Solution.BuildManager.Verify.ProjectBuilt(consoleProject);
                success &= VisualStudio.ObjectModel.Solution.BuildManager.Verify.Succeeded();
                success &= VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.Verify.ErrorCountIs(0);
                string[] errors = new string[] { };
                if (!success)
                {
                    VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.WaitForErrorListItems();
                    errors = VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.Errors.Select(x => $"Description:'{x.Description}' Project:{x.ProjectName} Line:'{x.LineNumber}'").ToArray();
                }

                Assert.IsTrue(success, $"project '{consoleProject.FileName}' failed to build.{Environment.NewLine}errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
        }
    }
}
