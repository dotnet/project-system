using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [ProjectSystemTrait]
    class CSharpSyntaxFactsServiceTests
    {
        private static ISyntaxFactsService s_service = new CSharpSyntaxFactsService(null);

        [Fact]
        public void TestGetModuleName()
        {
            Assert.Throws<NotImplementedException>(() => {
                s_service.GetModuleName(null);
            });
        }

        [Fact]
        public void TestIsModuleDeclaration()
        {
            Assert.False(s_service.IsModuleDeclaration(null));
        }

        [Fact]
        public void TestIsValidIdentifier()
        {
            Assert.True(s_service.IsValidIdentifier("Foo"));
            Assert.False(s_service.IsValidIdentifier("Foo`"));
        }
    }
}
