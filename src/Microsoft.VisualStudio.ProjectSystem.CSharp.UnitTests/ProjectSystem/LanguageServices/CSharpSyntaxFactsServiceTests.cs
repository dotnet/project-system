// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Trait("UnitTest", "ProjectSystem")]
    public class CSharpSyntaxFactsServiceTests
    {
        private static ISyntaxFactsService s_service = new CSharpSyntaxFactsService(null);

        [Fact]
        public void TestIsValidIdentifier()
        {
            Assert.True(s_service.IsValidIdentifier("Foo"));
            Assert.False(s_service.IsValidIdentifier("Foo`"));
        }
    }
}
