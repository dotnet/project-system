// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{

    [ProjectSystemTrait]
    public class VisualBasicSyntaxFactsServiceTests
    {
        private static ISyntaxFactsService s_service = new VisualBasicSyntaxFactsService(null);

        [Fact]
        public void TestIsValidIdentifier()
        {
            Assert.True(s_service.IsValidIdentifier("Foo"));
            Assert.False(s_service.IsValidIdentifier("Foo`"));
        }
    }
}
