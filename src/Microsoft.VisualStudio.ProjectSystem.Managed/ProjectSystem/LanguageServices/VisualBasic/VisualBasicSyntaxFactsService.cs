// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
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
