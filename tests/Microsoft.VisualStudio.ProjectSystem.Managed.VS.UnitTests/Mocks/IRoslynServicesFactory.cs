// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IRoslynServicesFactory
    {
        public static IRoslynServices Implement(ISyntaxFactsService syntaxFactsService)
        {
            var mock = new Mock<IRoslynServices>();

            mock.Setup(h => h.IsValidIdentifier(It.IsAny<string>()))
                .Returns<string>(name => syntaxFactsService.IsValidIdentifier(name));

            return mock.Object;
        }
    }
}
