// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices.VisualBasic;
using Microsoft.VisualStudio.Settings;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename.VisualBasic
{
    public class RenamerTests : RenamerTestsBase
    {
        protected override string ProjectFileExtension => "vbproj";

        [Theory]
        [InlineData("Bar.vb", "Foo.vb",
                    """
                    Class Foo
                    End Class
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Class Foo1
                    End Class
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Interface Foo1
                        Sub m1()
                        Sub m2()
                    End Interface
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Delegate Function Foo1(s As String) As Integer
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Partial Class Foo1
                    End Class
                    Partial Class Foo2
                    End Class
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Structure Foo1
                        Private author As String
                    End Structure
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Enum Foo1
                        None
                        enum1
                    End Enum
                    """)]
        [InlineData("Foo.vb", "Bar.vb",
                    """
                    Module Foo1
                    End Module
                    """)]
        [InlineData("Bar.vb", "Foo`.vb",
                    """
                    Class Foo
                    End Class
                    """)]
        [InlineData("Bar.vb", "Foo@.vb",
                    """
                    Class Foo
                    End Class
                    """)]
        [InlineData("Foo.vb", "Foo.vb",
                    """
                    Class Foo
                    End Class
                    """)]
        [InlineData("Foo.vb", "Folder1\\Foo.vb",
                    """
                    Class Foo
                    End Class
                    """)]
        // Change in case
        [InlineData("Foo.vb", "foo.vb",
                    """
                    Class Foo 
                    End Class
                    """)]
        [InlineData("Foo.vb", "Folder1\\foo.vb",
                    """
                    Class Foo
                    End Class
                    """)]
        public async Task Rename_Symbol_Should_Not_HappenAsync(string oldFilePath, string newFilePath, string sourceCode)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService());
            var vsOnlineServices = IVsOnlineServicesFactory.Create(online: false);
            var settingsManagerService = CreateSettingsManagerService(true);

            await RenameAsync(sourceCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, vsOnlineServices, LanguageNames.VisualBasic, settingsManagerService);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        [Theory]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Class Foo
            End Class
            """)]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Interface Foo
                Sub m1()
                Sub m2()
            End Interface
            """)]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Delegate Function Foo(s As String) As Integer
            """)]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Partial Class Foo
            End Class
            Partial Class Foo
            End Class
            """)]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Structure Foo
                Private author As String
            End Structure
            """)]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Enum Foo
                None
                enum1
            End Enum
            """)]
        [InlineData("Foo.vb", "Bar.vb",
            """
            Namespace n1
                Class Foo
                End Class
            End Namespace
            Namespace n2
                Class Foo
                End Class
            End Namespace
            """)]
        public async Task Rename_Symbol_Should_TriggerUserConfirmationAsync(string oldFilePath, string newFilePath, string sourceCode)
        {
            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService());
            var vsOnlineServices = IVsOnlineServicesFactory.Create(online: false);
            var settingsManagerService = CreateSettingsManagerService(true);

            await RenameAsync(sourceCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, vsOnlineServices, LanguageNames.VisualBasic, settingsManagerService);

            bool checkBoxSelection;
            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>(), out checkBoxSelection), Times.Once);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Rename_Symbol_Should_ExitEarlyInVSOnlineAsync()
        {
            string sourceCode =
                """
                Class Foo
                End Class
                """;
            string oldFilePath = "Foo.vb";
            string newFilePath = "Bar.vb";

            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService());
            var vsOnlineService = IVsOnlineServicesFactory.Create(online: true);
            var settingsManagerService = CreateSettingsManagerService(true);

            await RenameAsync(sourceCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, vsOnlineService, LanguageNames.VisualBasic, settingsManagerService);

            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        [Theory]
        [InlineData("Foo.vb", "Foo.txt")]
        [InlineData("Foo.vb", "Foo.vb2")]
        [InlineData("Foo.txt", "Foo.vb")]
        public async Task Rename_Symbol_Should_ExitEarlyInFileExtensionChange(string oldFilePath, string newFilePath)
        {
            string sourceCode =
                """
                Class Foo
                End Class
                """;
            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService());
            var vsOnlineService = IVsOnlineServicesFactory.Create(online: false);
            var settingsManagerService = CreateSettingsManagerService(true);

            await RenameAsync(sourceCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, vsOnlineService, LanguageNames.VisualBasic, settingsManagerService);
            bool disablePromptMessage;
            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>(), out disablePromptMessage), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        [Fact]
        public async Task Rename_Symbol_Should_ExitEarlyWhenFileDoesntChangeName()
        {
            string sourceCode =
                """
                Class Foo
                End Class
                """;
            string oldFilePath = "Foo.vb";
            string newFilePath = "FOO.vb";

            var userNotificationServices = IUserNotificationServicesFactory.Create();
            var roslynServices = IRoslynServicesFactory.Implement(new VisualBasicSyntaxFactsService());
            var vsOnlineService = IVsOnlineServicesFactory.Create(online: false);
            var settingsManagerService = CreateSettingsManagerService(true);

            await RenameAsync(sourceCode, oldFilePath, newFilePath, userNotificationServices, roslynServices, vsOnlineService, LanguageNames.VisualBasic, settingsManagerService);
            bool disablePromptMessage;
            Mock.Get(userNotificationServices).Verify(h => h.Confirm(It.IsAny<string>(), out disablePromptMessage), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.RenameSymbolAsync(It.IsAny<Solution>(), It.IsAny<ISymbol>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(roslynServices).Verify(h => h.ApplyChangesToSolution(It.IsAny<Workspace>(), It.IsAny<Solution>()), Times.Never);
        }

        private IVsService<SVsSettingsPersistenceManager, ISettingsManager> CreateSettingsManagerService(bool enableSymbolicRename)
        {
            var settingsManagerMock = new Mock<ISettingsManager>();

            settingsManagerMock.Setup(f => f.GetValueOrDefault("SolutionNavigator.EnableSymbolicRename", true))
                .Returns(enableSymbolicRename);

            return IVsServiceFactory.Create<SVsSettingsPersistenceManager, ISettingsManager>(settingsManagerMock.Object);
        }
    }
}
