// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.CSharp
{
    [Export(typeof(ISyntaxFactsService))]
    [Order(Order.Default)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpSyntaxFactsService : ISyntaxFactsService
    {
        [ImportingConstructor]
        public CSharpSyntaxFactsService()
        {
        }

        public bool IsValidIdentifier(string identifierName)
        {
            return SyntaxFacts.IsValidIdentifier(identifierName);
        }
    }
}
