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
        protected override string ProjectFileExtension => "vbproj";

        [Theory]
        [InlineData("Bar.vb", "Foo.vb",
                    @"Class Foo
                    End Class")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Class Foo1
                    End Class")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Interface Foo1
                      Sub m1()
                      Sub m2()
                    End Interface")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Delegate Function Foo1(s As String) As Integer")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Partial Class Foo1
                    End Class
                    Partial Class Foo2
                    End Class")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Structure Foo1
                        Private author As String
                    End Structure")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Enum Foo1
                        None
                        enum1
                    End Enum")]
        [InlineData("Foo.vb", "Bar.vb",
                    @"Module Foo1
                    End Module")]
        [InlineData("Bar.vb", "Foo`.vb",
                    @"Class Foo
                    End Class")]
        [InlineData("Bar.vb", "Foo@.vb",
                    @"Class Foo
                    End Class")]
        [InlineData("Foo.vb", "Foo.vb",
                    @"Class Foo
                    End Class")]
        [InlineData("Foo.vb", "Folder1\\Foo.vb",
                    @"Class Foo
                    End Class")]
        // Change in case
        [InlineData("Foo.vb", "foo.vb",
                    @"Class Foo 
                      End Class")]
        [InlineData("Foo.vb", "Folder1\\foo.vb",
                    @"Class Foo
                      End Class")]
        public async Task Rename_Symbol_Should_Not_HappenAsync(string oldFilePath, string newFilePath, string soureCode)
        {

            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService(null));

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.VisualBasic);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        [Theory]
        [InlineData("Foo.vb", "Bar.vb",
            @"Class Foo
            End Class")]
        [InlineData("Foo.vb", "Bar.vb",
            @"Interface Foo
                Sub m1()
                Sub m2()
            End Interface")]
        [InlineData("Foo.vb", "Bar.vb",
            @"Delegate Function Foo(s As String) As Integer")]
        [InlineData("Foo.vb", "Bar.vb",
            @"Partial Class Foo
            End Class
            Partial Class Foo
            End Class")]
        [InlineData("Foo.vb", "Bar.vb",
            @"Structure Foo
                Private author As String
            End Structure")]
        [InlineData("Foo.vb", "Bar.vb",
            @"Enum Foo
                None
                enum1
            End Enum")]
        [InlineData("Foo.vb", "Bar.vb",
            @"Namespace n1
                Class Foo
                End Class
            End Namespace
            Namespace n2
                Class Foo
                End Class
            End Namespace")]
        public async Task Rename_Symbol_Should_TriggerUserConfirmationAsync(string oldFilePath, string newFilePath, string soureCode)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService(null));

            await RenameAsync(soureCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, LanguageNames.VisualBasic);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Once);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>()), Times.Never);
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
