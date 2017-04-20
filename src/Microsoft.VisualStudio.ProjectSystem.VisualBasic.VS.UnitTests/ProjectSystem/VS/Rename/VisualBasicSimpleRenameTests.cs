using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [ProjectSystemTrait]
    public class VisualBasicSimpleRenameTests : SimpleRenamerTestsBase
    {
        protected override string TempFileName => "temp.vb";

        protected override string DefaultFile => "Class temp" +
            "                                     End Class";

        protected override string ProjectFileExtension => "vbproj";

        [Theory]
        [InlineData(@"Class Foo 
                      End Class", "Foo.vb", "foo.vb")]
        [InlineData(@"Class Foo
                     End Class", "Foo.vb", "Folder1\\foo.vb")]
        public async Task Rename_Symbol_Should_Not_HappenAsync_ForVB(string soureCode, string oldFilePath, string newFilePath)
        {

            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService(null));

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.VisualBasic);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        internal async Task RenameAsync(string soureCode, string oldFilePath, string newFilePath, IUserNotificationServices userNotificationServices, IRoslynServices roslynServices, string language)
        {
            using (var ws = new AdhocWorkspace())
            {
                var projectId = ProjectId.CreateNewId();
                Solution solution = ws.AddSolution(InitializeWorkspace(projectId, newFilePath, soureCode, language));
                Project project = (from d in solution.Projects where d.Id == projectId select d).FirstOrDefault();

                var environmentOptionsFactory = IEnvironmentOptionsFactory.Implement((string category, string page, string property, bool defaultValue) => { return true; });

                var renamer = new Renamer(ws, IProjectThreadingServiceFactory.Create(), userNotificationServices, environmentOptionsFactory, roslynServices, project, oldFilePath, newFilePath);
                await renamer.RenameAsync(project);
            }
        }
    }
}
