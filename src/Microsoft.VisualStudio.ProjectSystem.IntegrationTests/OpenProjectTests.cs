using System;
using System.Linq;
using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [TestClass]
    public class CreateProjectTests : VisualStudioHostTest
    {
        private static string _hiveName;

        [ClassInitialize()]
        public static void ClassInitialize(TestContext context)
        {
            _hiveName = context.Properties["VsRootSuffix"].ToString();
        }

        protected VisualStudioHostConfiguration DefaultHostConfiguration
        {
            get
            {
                var visualStudioHostConfiguration = new VisualStudioHostConfiguration()
                {
                    CommandLineArguments = $"/rootSuffix {_hiveName}",
                    RestoreUserSettings = false,
                    InheritProcessEnvironment = true,
                    AutomaticallyDismissMessageBoxes = true,
                    DelayInitialVsLicenseValidation = true,
                    ForceFirstLaunch = true,
                };

                return visualStudioHostConfiguration;
            }
        }

        protected VisualStudioHost GetVS() => Operations.CreateHost<VisualStudioHost>(DefaultHostConfiguration);

        [TestMethod]
        public void CreateProject_CreateAndBuild()
        {
            var VS = GetVS();

            VS.Start();

            ProjectTestExtension consoleProject = default;
            using (Scope.Enter("Create Project"))
            {
                consoleProject = VS.ObjectModel.Solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                VS.ObjectModel.Solution.Verify.HasProject();
            }

            using (Scope.Enter("Build Project"))
            {
                VS.ObjectModel.Solution.Build();
            }

            using (Scope.Enter("Verify Build Succeeded"))
            {
                var sucess = VS.ObjectModel.Solution.BuildManager.Verify.ProjectBuilt(consoleProject);
                string[] errors = new string[] { };
                if (!sucess)
                {
                    VS.ObjectModel.Shell.ToolWindows.ErrorList.WaitForErrorListItems();
                    errors = VS.ObjectModel.Shell.ToolWindows.ErrorList.Errors.Select(x => $"Description:'{x.Description}' Project:{x.ProjectName} Line:'{x.LineNumber}'").ToArray();
                }

                Assert.IsTrue(sucess, $"project '{consoleProject.FileName}' failed to build.{Environment.NewLine}errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
            }
        }
    }
}
