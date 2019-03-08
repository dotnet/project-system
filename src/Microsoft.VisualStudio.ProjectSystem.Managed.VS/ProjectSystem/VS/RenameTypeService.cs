// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

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

        private ISyntaxFactsService SyntaxFactsService
        {
            get
            {
                return SyntaxFactsServicesImpl.FirstOrDefault()?.Value;
            }
        }

        public async Task<bool> AnyTypeToRenameAsync(string oldFilePath, string newFilePath, string projectPath)
        {
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            if (!TryGetProjectAtPath(projectPath, out Project project))
                return false;

            if (!await CanHandleRenameAsync(oldName, newName, project))
                return false;

            (bool success, ISymbol symbol) = await TryGetSymbolToRenameAsync(oldName, newFilePath, project);
            return success && symbol != null;
        }

        public bool RenameType(string oldFilePath, string newFilePath, string projectPath, CancellationToken cancellationToken)
        {
            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            DocumentId documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(newFilePath).FirstOrDefault();
            if (documentId is null)
                return false;

            EnvDTE.FileCodeModel codeModel = _workspace.GetFileCodeModel(documentId);

            if (!TryGetCodeElementToRename(oldName, codeModel.CodeElements, out EnvDTE80.CodeElement2 codeElementToRename))
                return false;

            codeElementToRename.RenameSymbol(newName);
            return true;
        }

        private bool TryGetCodeModel(string newFilePath, Project project, out EnvDTE.FileCodeModel codeModel)
        {
            codeModel = null;
            Document newDocument = GetDocument(project, newFilePath);
            if (newDocument is null)
                return false;

            IVsHierarchy hierarchy = _workspace.GetHierarchy(project.Id);
            if (hierarchy is null)
                return false;

            if (ErrorHandler.Failed(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object value)))
                return false;

            var dteProject = (EnvDTE.Project)value;

            EnvDTE.ProjectItem projectItemForDocument = dteProject.ProjectItems
                .OfType<EnvDTE.ProjectItem>()
                .FirstOrDefault(p => StringComparer.InvariantCultureIgnoreCase.Compare(p.Name, newDocument.Name) == 0);

            codeModel = projectItemForDocument.FileCodeModel;
            return true;
        }

        private bool TryGetProjectAtPath(string fullPath, out Project project)
        {
            // NOTE: It is safe to grab the first project we encounter which
            // contains the given file. Roslyn will handle the case where the
            // file is included in multiple projects (linked-files or multi-TFM)
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

        private static bool  TryGetCodeElementToRename(string oldName,
                                                       EnvDTE.CodeElements elements,
                                                       bool isCaseSensitive,
                                                       out EnvDTE80.CodeElement2 codeElementToRename)
        {
            foreach (EnvDTE.CodeElement element in elements)
            {
                if (element.Kind == EnvDTE.vsCMElement.vsCMElementNamespace)
                {
                    if (TryGetCodeElementToRename(oldName, element.Children, isCaseSensitive, out codeElementToRename))
                    {
                        return true;
                    }
                }

                if (element.Kind == EnvDTE.vsCMElement.vsCMElementStruct ||
                    element.Kind == EnvDTE.vsCMElement.vsCMElementClass ||
                    element.Kind == EnvDTE.vsCMElement.vsCMElementModule ||
                    element.Kind == EnvDTE.vsCMElement.vsCMElementInterface ||
                    element.Kind == EnvDTE.vsCMElement.vsCMElementEnum)
                {
                    string elementName = element.Name;
                    if(string.Equals(oldName, elementName, isCaseSensitive ? StringComparison.Ordinal: StringComparison.OrdinalIgnoreCase))
                    {
                        codeElementToRename = element as EnvDTE80.CodeElement2;
                        return true;
                    }
                }
            }

            codeElementToRename = null;
            return false;
        }

        private static async Task<(bool success, ISymbol symbolToRename)> TryGetSymbolToRenameAsync(string oldName,
            string newFileName,
            Project project)
        {
            if (project is null)
                return (false, null);

            Document newDocument = GetDocument(project, newFileName);
            if (newDocument is null)
                return (false, null);

            SyntaxNode root = await GetRootNode(newDocument);
            if (root is null)
                return (false, null);

            SemanticModel semanticModel = await newDocument.GetSemanticModelAsync();
            if (semanticModel is null)
                return (false, null);

            IEnumerable<SyntaxNode> declarations = root.DescendantNodes().Where(n => HasMatchingSyntaxNode(semanticModel, n, oldName, semanticModel.Compilation.IsCaseSensitive));
            SyntaxNode declaration = declarations.FirstOrDefault();
            if (declaration is null)
                return (false, null);

            return (true, semanticModel.GetDeclaredSymbol(declaration));

        }

        private static Document GetDocument(Project project, string filePath)
            => (from d in project.Documents where StringComparers.Paths.Equals(d.FilePath, filePath) select d).FirstOrDefault();

        private static Task<SyntaxNode> GetRootNode(Document newDocument, CancellationToken token = default) => newDocument.GetSyntaxRootAsync(token);

        private static bool HasMatchingSyntaxNode(SemanticModel model, SyntaxNode syntaxNode, string name, bool isCaseSensitive, CancellationToken token = default)
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
