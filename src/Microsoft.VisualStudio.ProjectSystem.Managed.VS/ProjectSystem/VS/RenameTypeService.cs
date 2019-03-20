// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IRenameTypeService))]
    internal class RenameTypeService : IRenameTypeService
    {
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public RenameTypeService(
            UnconfiguredProject project,
            VisualStudioWorkspace visualStudioWorkspace)
        {
            _workspace = visualStudioWorkspace;
            SyntaxFactsServicesImpl = new OrderPrecedenceImportCollection<ISyntaxFactsService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ISyntaxFactsService> SyntaxFactsServicesImpl { get; }

        private ISyntaxFactsService SyntaxFactsService => SyntaxFactsServicesImpl.First().Value;

        public async Task<bool> AnyTypeToRenameAsync(string oldFilePath, string newFilePath)
        {
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            if (!CanHandleRename(oldName, newName))
                return false;

            ISymbol symbol = await TryGetSymbolToRenameAsync(oldName, newFilePath);
            return symbol != null;
        }

        public bool RenameType(string oldFilePath, string newFilePath, CancellationToken cancellationToken)
        {
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            if (!TryGetDocument(newFilePath, out Document document))
                return false;

            EnvDTE.FileCodeModel codeModel = _workspace.GetFileCodeModel(document.Id);

            if (!TryGetCodeElementToRename(oldName, codeModel.CodeElements, out EnvDTE80.CodeElement2 codeElementToRename))
                return false;

            codeElementToRename.RenameSymbol(newName);
            return true;
        }

        private bool CanHandleRename(string oldName, string newName)
            => IsValidIdentifier(oldName) &&
               IsValidIdentifier(newName) &&
               AreNotSameIdentifierName(oldName, newName);

        private bool IsValidIdentifier(string identifierName)
            => SyntaxFactsService.IsValidIdentifier(identifierName);

        private bool AreNotSameIdentifierName(string oldName, string newName)
            => !AreSameIdentifierName(oldName, newName);

        private bool AreSameIdentifierName(string oldName, string newName)
            => SyntaxFactsService.StringComparer.Equals(oldName, newName);

        private bool TryGetCodeElementToRename(string oldName,
                                               EnvDTE.CodeElements elements,
                                               out EnvDTE80.CodeElement2 codeElementToRename)
        {
            foreach (EnvDTE.CodeElement element in elements)
            {
                // Return the code element if it is a type with the same name
                if ((element.Kind == EnvDTE.vsCMElement.vsCMElementStruct ||
                     element.Kind == EnvDTE.vsCMElement.vsCMElementClass ||
                     element.Kind == EnvDTE.vsCMElement.vsCMElementModule ||
                     element.Kind == EnvDTE.vsCMElement.vsCMElementInterface ||
                     element.Kind == EnvDTE.vsCMElement.vsCMElementEnum) &&
                     SyntaxFactsService.StringComparer.Equals(oldName, element.Name) &&
                     element is EnvDTE80.CodeElement2 element2)
                {
                    codeElementToRename = element2;
                    return true;
                }

                // If no matching names were found recurse into namespace-like and type-like definitions
                else if ((element.Kind == EnvDTE.vsCMElement.vsCMElementNamespace ||
                          element.Kind == EnvDTE.vsCMElement.vsCMElementClass ||
                          element.Kind == EnvDTE.vsCMElement.vsCMElementModule ||
                          element.Kind == EnvDTE.vsCMElement.vsCMElementInterface) &&
                          TryGetCodeElementToRename(oldName, element.Children, out codeElementToRename))
                {
                    return true;
                }
            }

            // No elements were found with a matching name
            codeElementToRename = null;
            return false;
        }

        private async Task<ISymbol> TryGetSymbolToRenameAsync(string name,
                                                              string filePath,
                                                              CancellationToken cancellationToken = default)
        {
            if (!TryGetDocument(filePath, out Document newDocument))
                return null;

            SyntaxNode root = await newDocument.GetSyntaxRootAsync(cancellationToken);
            if (root is null)
                return null;

            SemanticModel semanticModel = await newDocument.GetSemanticModelAsync();
            if (semanticModel is null)
                return null;

            IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(semanticModel, n, name, cancellationToken));
            SyntaxNode declaration = declarations.FirstOrDefault();
            if (declaration is null)
                return null;

            return semanticModel.GetDeclaredSymbol(declaration);
        }

        private bool TryGetDocument(string filePath, out Document document)
        {
            // NOTE: It is safe to grab the first project we encounter which
            // contains the given file. Roslyn will handle the case where the
            // file is included in multiple projects (linked-files or multi-TFM)
            Solution solution = _workspace.CurrentSolution;
            DocumentId id = solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
            if (id is null)
            {
                document = null;
                return false;
            }

            document = solution.GetDocument(id);
            return document != null;
        }

        private bool HasMatchingSyntaxNode(SemanticModel model, SyntaxNode syntaxNode, string name, CancellationToken cancellationToken = default)
        {
            if (model.GetDeclaredSymbol(syntaxNode, cancellationToken) is INamedTypeSymbol symbol)
            {
                return AreSameIdentifierName(symbol.Name, name);
            }

            return false;
        }
    }
}
