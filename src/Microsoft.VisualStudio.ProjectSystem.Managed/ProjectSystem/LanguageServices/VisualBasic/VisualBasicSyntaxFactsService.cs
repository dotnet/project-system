// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.VisualBasic
{
    [Export(typeof(ISyntaxFactsService))]
    [Order(Order.Default)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicSyntaxFactsService : ISyntaxFactsService
    {
        [ImportingConstructor]
        public VisualBasicSyntaxFactsService()
        {
        }

        public bool IsValidIdentifier(string identifierName)
        {
            return SyntaxFacts.IsValidIdentifier(identifierName);
        }
    }
}
