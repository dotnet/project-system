// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

using RoslynRenamer = Microsoft.CodeAnalysis.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IRoslynServices))]
    internal class RoslynServices : IRoslynServices
    {
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public RoslynServices(
            UnconfiguredProject project,
            VisualStudioWorkspace visualStudioWorkspace)
        {
            _workspace = visualStudioWorkspace;
            SyntaxFactsServicesImpl = new OrderPrecedenceImportCollection<ISyntaxFactsService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ISyntaxFactsService> SyntaxFactsServicesImpl { get; }

        private ISyntaxFactsService SyntaxFactsService
        {
            get
            {
                return SyntaxFactsServicesImpl.FirstOrDefault()?.Value;
            }
        }

        public async Task<bool> AnyTypeToRenameAsync(string oldName, string newName, string filePath)
        {
            if (!TryGetProjectAtPath(filePath, out Project project))
                return false;

            if (!await CanHandleRenameAsync(oldName, newName, project))
                return false;

            (bool success, _) = await TryGetSymbolToRenameAsync(oldName, filePath, project, default);
            return success;
        }

        public async Task<bool> RenameTypeAsync(string oldName, string newName, string filePath, CancellationToken cancellationToken)
        {
            if (!TryGetProjectAtPath(filePath, out Project project))
                return false;

            (bool success, ISymbol symbol) = await TryGetSymbolToRenameAsync(oldName, filePath, project, cancellationToken);
            if (!success)
                return false;

            Solution renamedSolution = await RoslynRenamer.Renamer.RenameSymbolAsync(project.Solution, symbol, newName, project.Solution.Workspace.Options, cancellationToken);
            return _workspace.TryApplyChanges(renamedSolution);
        }

        private bool TryGetProjectAtPath(string fullPath, out Project project)
        {
            foreach (Project proj in _workspace.CurrentSolution.Projects)
            {
                if (StringComparers.Paths.Equals(proj.FilePath, fullPath))
                {
                    project = proj;
                    return true;
                }

            }

            project = null;
            return false;
        }

        private async Task<bool> CanHandleRenameAsync(string oldName, string newName, Project project)
        {
            (bool success, bool isCaseSensitive) = await TryDetermineIfCompilationIsCaseSensitiveAsync(project);
            if (!success)
                return false;

            return IsValidIdentifier(oldName) &&
                   IsValidIdentifier(newName) &&
                       (!string.Equals(
                            oldName,
                            newName,
                            isCaseSensitive
                                ? StringComparison.Ordinal
                                : StringComparison.OrdinalIgnoreCase));
        }

        private bool IsValidIdentifier(string identifierName)
        {
            return SyntaxFactsService?.IsValidIdentifier(identifierName) ?? false;
        }

        private static async Task<(bool success, bool isCaseSensitive)> TryDetermineIfCompilationIsCaseSensitiveAsync(Project project)
        {
            Compilation compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                // this project does not support compilations
                return (false, false);
            }

            return (true, compilation.IsCaseSensitive);
        }

        private static async Task<(bool success, ISymbol symbolToRename)> TryGetSymbolToRenameAsync(string oldName,
                                                                                                    string newFileName,
                                                                                                    Project project,
                                                                                                    CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (project is null)
                return (false, null);

            Document newDocument = GetDocument(project, newFileName);
            if (newDocument is null)
                return (false, null);

            SyntaxNode root = await GetRootNode(newDocument, token);
            if (root is null)
                return (false, null);

            SemanticModel semanticModel = await newDocument.GetSemanticModelAsync(token);
            if (semanticModel is null)
                return (false, null);

            IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(semanticModel, n, oldName, semanticModel.Compilation.IsCaseSensitive, token));
            SyntaxNode declaration = declarations.FirstOrDefault();
            if (declaration is null)
                return (false, null);

            return (true, semanticModel.GetDeclaredSymbol(declaration, token));

        }

        private static Document GetDocument(Project project, string filePath)
            => (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, filePath) select d).FirstOrDefault();

        private static Task<SyntaxNode> GetRootNode(Document newDocument, CancellationToken token) => newDocument.GetSyntaxRootAsync(token);

        private static bool HasMatchingSyntaxNode(SemanticModel model, SyntaxNode syntaxNode, string name, bool isCaseSensitive, CancellationToken token)
        {
            if (model.GetDeclaredSymbol(syntaxNode, token) is INamedTypeSymbol symbol &&
                (symbol.TypeKind == TypeKind.Class
                 || symbol.TypeKind == TypeKind.Interface
                 || symbol.TypeKind == TypeKind.Delegate
                 || symbol.TypeKind == TypeKind.Enum
                 || symbol.TypeKind == TypeKind.Struct
                 || symbol.TypeKind == TypeKind.Module))
            {
                return string.Compare(symbol.Name, name, !isCaseSensitive) == 0;
            }

            return false;
        }
    }
}
