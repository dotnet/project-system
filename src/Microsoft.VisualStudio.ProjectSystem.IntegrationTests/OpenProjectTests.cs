using Microsoft.Test.Apex.VisualStudio;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.IntegrationTests
{
    [TestClass]
    public class CreateProjectTests : VisualStudioHostTest
    {
        [TestMethod]
        public void CreateProject_CreateRestoreAndBuild()
        {
            var proj = VisualStudio.ObjectModel.Solution.CreateProject(ProjectLanguage.CSharp, ProjectTemplate.ClassLibrary);
            VisualStudio.ObjectModel.Solution.Verify.HasProject();
            VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.WaitForReady();
            VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.WaitForNoErrorListItems();
            VisualStudio.ObjectModel.Solution.Build();
            VisualStudio.ObjectModel.Shell.ToolWindows.ErrorList.Verify.ErrorCountIs(0);
        }
    }
}
