// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [TestClass]
    public class CreateProjectTests : TestBase
    {
        [TestMethod]
        public void CreateProject_CreateAndBuild()
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

            using (Scope.Enter("Build Project"))
            {
                VisualStudio.ObjectModel.Solution.Build();
            }

            using (Scope.Enter("Verify Build Succeeded"))
            {
                var success = VisualStudio.ObjectModel.Solution.BuildManager.Verify.ProjectBuilt(consoleProject);
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
