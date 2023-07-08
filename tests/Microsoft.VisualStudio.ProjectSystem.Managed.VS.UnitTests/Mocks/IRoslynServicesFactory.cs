// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IRoslynServicesFactory
    {
        public static IRoslynServices Implement(ISyntaxFactsService syntaxFactsService)
        {
            var mock = new Mock<IRoslynServices>();

            mock.Setup(h => h.IsValidIdentifier(It.IsAny<string>()))
                .Returns<string>(syntaxFactsService.IsValidIdentifier);

            return mock.Object;
        }
    }
}
