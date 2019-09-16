// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
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
