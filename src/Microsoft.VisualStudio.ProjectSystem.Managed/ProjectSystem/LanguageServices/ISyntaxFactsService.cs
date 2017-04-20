// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// A Language service for Unconfigured Projects that can answer questions about language syntax
    /// </summary>
    internal interface ISyntaxFactsService
    {
        bool IsValidIdentifier(string identifierName);
        bool IsModuleDeclaration(SyntaxNode identifierName);
        string GetModuleName(SyntaxNode syntaxNode);
    }
}
