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
                    CommandLineArguments = $"/rootSuffix:{_hiveName}",
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

            using (Scope.Enter("Create Project"))
            {
                var proj = VS.ObjectModel.Solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.NetCoreConsoleApp);
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
                VS.ObjectModel.Shell.ToolWindows.ErrorList.Verify.ErrorCountIs(0);
            }
        }
    }
}
