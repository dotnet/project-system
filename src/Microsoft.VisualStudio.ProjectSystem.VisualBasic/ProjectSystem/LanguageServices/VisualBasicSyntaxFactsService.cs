// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(ISyntaxFactsService))]
    [Order(Order.Default)]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicSyntaxFactsService : ISyntaxFactsService
    {
        [ImportingConstructor]
        public VisualBasicSyntaxFactsService(UnconfiguredProject project)
        {
        }

        public string GetModuleName(SyntaxNode syntaxNode)
        {
            var vbNode = (VisualBasicSyntaxNode)syntaxNode;
            if (vbNode.Kind() != SyntaxKind.ModuleBlock)
            {
                return null;
            }

            var moduleBlock = (ModuleBlockSyntax)vbNode;
            if (!moduleBlock.ModuleStatement.IsMissing &&
                !moduleBlock.ModuleStatement.Identifier.IsMissing)
            {
                return moduleBlock.ModuleStatement.Identifier.ValueText;
            }

            return null;
        }

        public bool IsModuleDeclaration(SyntaxNode node)
        {
            return ((VisualBasicSyntaxNode)node).Kind() == SyntaxKind.ModuleBlock;
        }

        public bool IsValidIdentifier(string identifierName)
        {
            return SyntaxFacts.IsValidIdentifier(identifierName);
        }
    }
}
