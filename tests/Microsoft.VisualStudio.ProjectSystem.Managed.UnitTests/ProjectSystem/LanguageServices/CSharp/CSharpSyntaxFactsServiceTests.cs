// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.CSharp
{
    public class CSharpSyntaxFactsServiceTests
    {
        private static readonly ISyntaxFactsService s_service = new CSharpSyntaxFactsService();

        [Fact]
        public void TestIsValidIdentifier()
        {
            Assert.True(s_service.IsValidIdentifier("Foo"));
            Assert.False(s_service.IsValidIdentifier("Foo`"));
        }
    }
}
