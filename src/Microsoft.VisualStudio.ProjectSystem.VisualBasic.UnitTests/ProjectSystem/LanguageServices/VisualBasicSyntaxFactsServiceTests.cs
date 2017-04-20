using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{

    [ProjectSystemTrait]
    public class VisualBasicSyntaxFactsServiceTests
    {
        private static ISyntaxFactsService s_service = new VisualBasicSyntaxFactsService(null);

        [Fact]
        public void TestGetModuleName()
        {
            var syntax = SyntaxFactory.ModuleBlock(
                            SyntaxFactory.ModuleStatement(
                                SyntaxFactory.Identifier("Foo")));
            Assert.True(string.Compare("Foo", s_service.GetModuleName(syntax)) == 0);
        }

        [Fact]
        public void TestIsModuleDeclaration()
        {
            var moduleStatementSyntax = SyntaxFactory.ModuleStatement(SyntaxFactory.Identifier("Foo"));
            var syntax = SyntaxFactory.ModuleBlock(
                            moduleStatementSyntax);
            Assert.True(s_service.IsModuleDeclaration(syntax));
            Assert.False(s_service.IsModuleDeclaration(moduleStatementSyntax));
        }

        [Fact]
        public void TestIsValidIdentifier()
        {
            Assert.True(s_service.IsValidIdentifier("Foo"));
            Assert.False(s_service.IsValidIdentifier("Foo`"));
        }
    }
}
