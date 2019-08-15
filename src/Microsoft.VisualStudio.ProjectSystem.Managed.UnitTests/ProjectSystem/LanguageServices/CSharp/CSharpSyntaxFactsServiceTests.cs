// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

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
