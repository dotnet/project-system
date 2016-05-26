//--------------------------------------------------------------------------------------------
// FileRenameTracker
//
// Exports an IProjectChangeHintReceiver to listen to file renames. If the file being renamed
// is a code file, it will prompt the user to rename the class to match. The rename is done
// using code model
//
// Copyright(c) 2015 Microsoft Corporation
//--------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectChangeHintReceiver)), Export]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityRenameHint.RenamedFileAsString)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class FileRenameTracker : IProjectChangeHintReceiver
    {

        private IComponentModel componentModel;
        private VisualStudioWorkspace visualStudioWorkspace;

        protected readonly IUnconfiguredProjectVsServices UnconfiguredProjectVsServices;
        /// <summary>
        /// The thread handling service.
        /// </summary>
        [Import]
        private IProjectThreadingService ThreadingService
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the VS global service provider.
        /// </summary>
        [Import]
        protected SVsServiceProvider ServiceProvider { get; private set; }

        [ImportingConstructor]
        public FileRenameTracker(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            UnconfiguredProjectVsServices = projectVsServices;
        }

        public async Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {
            var files = hints.GetValueOrDefault(ProjectChangeFileSystemEntityRenameHint.RenamedFile) ?? ImmutableHashSet.Create<IProjectChangeHint>();
            if (files.Count == 1)
            {
                var hint = files.First() as IProjectChangeFileRenameHint;
                if (hint != null && !hint.ChangeAlreadyOccurred)
                {
                    var kvp = hint.RenamedFiles.First();
                    await DoRenameClassAsync(kvp.Key, kvp.Value).ConfigureAwait(false);
                }
            }
        }

        public Task HintingAsync(IProjectChangeHint hint)
        {
            return TplExtensions.CompletedTask;
        }

        /// <summary>
        /// static helper method to do the class rename. Must be called on the UI thread
        /// </summary>
        private async Task DoRenameClassAsync(string oldFilePath, string newFilePath)
        {
            

            string codeExtension = Path.GetExtension(newFilePath);
            if (codeExtension == null || !oldFilePath.EndsWith(codeExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            await ThreadingService.SwitchToUIThread();
            if (visualStudioWorkspace == null )
            {
                componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
                visualStudioWorkspace = componentModel.GetService<VisualStudioWorkspace>();
            }
           
            IVsHierarchy hierarchy = UnconfiguredProjectVsServices.Hierarchy;
            EnvDTE.Project project = hierarchy.GetDTEProject();
            var myProject = visualStudioWorkspace
                .CurrentSolution
                .Projects.Where(p => String.Equals(p.FilePath, project.FullName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

      
            var TemporaryTimmer = Task.Delay(10 * 1000).ContinueWith(async (_) =>
            {
                Document newDocument = (from d in visualStudioWorkspace.CurrentSolution.GetProject(myProject.Id).Documents where d.FilePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase) select d).FirstOrDefault();
                if (newDocument == null)
                    return;

                var root = await newDocument.GetSyntaxRootAsync().ConfigureAwait(false);
                if (root == null)
                    return;

                var declaration = root.DescendantNodes().Where(n => IsTypeNode(n, oldName)).FirstOrDefault();
                if (declaration == null)
                    return;

                var semanticModel = await newDocument.GetSemanticModelAsync().ConfigureAwait(false);
                if (semanticModel == null)
                    return;
                
                var symbol = semanticModel.GetDeclaredSymbol(declaration);
                if (symbol == null)
                    return;

                // We're about to do a symbolic rename.If the user has asked us to prompt, We need to open a dialog and ask them  
                //  Otherwise go ahead and do the rename.
                EnvDTE.DTE dte = ServiceProvider.GetService<EnvDTE.DTE, EnvDTE.DTE>();
                var props = dte.Properties["Environment", "ProjectsAndSolution"];
                if (props != null)
                {
                    if ((bool)props.Item("PromptForRenameSymbol").Value)
                    {
                        string promptMessage = string.Format(Resources.RenameSymbolPrompt, oldName);
                        var result = VsShellUtilities.ShowMessageBox(ServiceProvider, promptMessage, null, OLEMSGICON.OLEMSGICON_QUERY,
                                                        OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        if (result == (int)VSConstants.MessageBoxResult.IDNO)
                        {
                            return;
                        }
                    }
                }

                // Now do the rename
                var optionSet = newDocument.Project.Solution.Workspace.Options;
                var renamedSolution = await Renamer.RenameSymbolAsync(newDocument.Project.Solution, symbol, newName, optionSet).ConfigureAwait(false);
                
                await ThreadingService.SwitchToUIThread();

                if (!newDocument.Project.Solution.Workspace.TryApplyChanges(renamedSolution))
                {
                    string promptMessage = string.Format(Resources.RenameSymbolFailed, oldName);
                    var result = VsShellUtilities.ShowMessageBox(ServiceProvider, promptMessage, null, OLEMSGICON.OLEMSGICON_WARNING,
                                                     OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private bool IsTypeNode(SyntaxNode syntax, string oldName)
        {
            if (syntax is BaseTypeDeclarationSyntax || syntax is DelegateDeclarationSyntax)
            {
                switch (syntax.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                        var classDecl = (ClassDeclarationSyntax)syntax;
                        if (classDecl.Identifier.ToString() == oldName)
                        {
                            return true;
                        }
                        break;


                    default:
                        break;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Returns the code element that matches the name, or null 
        /// </summary>
        private EnvDTE.CodeElement GetCodeElementByName(EnvDTE.CodeElements codeElements, string elementName,
                                                bool recurse = true, bool caseSensitiveComparison = true)
        {
            // We don't want this function to throw. It is unclear which codemodel functions will throw instead of
            // returning null
            // Loop through each element to get the required info.  The indexes are 1-based.
            int elementCount = codeElements.Count;
            foreach (var element in codeElements)
            {
                if (element is EnvDTE.CodeElement)
                {
                    EnvDTE.CodeElement codeElement = (EnvDTE.CodeElement)element;
                    var kind = codeElement.Kind;
                    if (kind == EnvDTE.vsCMElement.vsCMElementStruct || kind == EnvDTE.vsCMElement.vsCMElementClass ||
                        kind == EnvDTE.vsCMElement.vsCMElementModule || kind == EnvDTE.vsCMElement.vsCMElementInterface ||
                        kind == EnvDTE.vsCMElement.vsCMElementEnum)
                    {
                        string fullName = codeElement.FullName;
                        if (fullName.Equals(elementName, caseSensitiveComparison ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                        {
                            return codeElement;
                        }
                    }

                    // Items can contain children.  We recurse if requested
                    if (recurse)
                    {
                        EnvDTE.CodeElements childElements = null;

                        if (element is EnvDTE.CodeType)
                        {
                            childElements = ((EnvDTE.CodeType)element).Members;
                        }
                        else if (element is EnvDTE.CodeNamespace)
                        {
                            childElements = ((EnvDTE.CodeNamespace)element).Members;
                        }

                        if (childElements != null)
                        {
                            codeElement = GetCodeElementByName(childElements, elementName, recurse, caseSensitiveComparison);
                            if (codeElement != null)
                            {
                                return codeElement;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
