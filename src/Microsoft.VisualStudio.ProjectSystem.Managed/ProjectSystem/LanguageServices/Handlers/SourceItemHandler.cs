// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    /// <summary>
    ///     Handles changes to sources files during project evaluations and changes to source files that are passed
    ///     to the compiler during design-time builds.
    /// </summary>
    [Export(typeof(ILanguageServiceCommandLineHandler))]
    [Export(typeof(ILanguageServiceRuleHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class SourceItemHandler : AbstractLanguageServiceRuleHandler, ILanguageServiceCommandLineHandler
    {
        // When a source file has been added/removed from a project, we'll receive notifications for it twice; once
        // during project evaluation, and once during a design-time build. To prevent us from adding duplicate items 
        // to the language service, we remember what sources files we've already added.
        //
        // We listen to both project evaluation and design-time builds ("command-line arguments") so that when
        // a file is added to the project, we don't need to wait for a slow design-time build just to get useful 
        // IntelliSense.
        private readonly UnconfiguredProject _project;
        private readonly Dictionary<IWorkspaceProjectContext, HashSet<string>> _sourceFilesByContext = new Dictionary<IWorkspaceProjectContext, HashSet<string>>();
        private readonly IPhysicalProjectTree _projectTree;

        [ImportingConstructor]
        public SourceItemHandler(UnconfiguredProject project, IPhysicalProjectTree projectTree)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(projectTree, nameof(projectTree));

            _project = project;
            _projectTree = projectTree;
        }

        public override string RuleName
        {
            get { return CSharp.SchemaName; }
        }

        public override RuleHandlerType HandlerType
        {
            get { return RuleHandlerType.Evaluation; }
        }

        public void Handle(CommandLineArguments added, CommandLineArguments removed, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(added, nameof(added));
            Requires.NotNull(removed, nameof(removed));

            foreach (CommandLineSourceFile sourceFile in removed.SourceFiles)
            {
                RemoveSourceFile(sourceFile.Path, context);
            }

            foreach (CommandLineSourceFile sourceFile in added.SourceFiles)
            {
                AddSourceFile(sourceFile.Path, null, context, isActiveContext);
            }
        }

        public override void Handle(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(projectChange, nameof(projectChange));

            IProjectChangeDiff diff = projectChange.Difference;

            foreach (string filePath in diff.RemovedItems)
            {
                RemoveSourceFile(filePath, context);
            }

            if (diff.AddedItems.Count > 0 || diff.RenamedItems.Count > 0 || diff.ChangedItems.Count > 0)
            {
                foreach (string filePath in diff.AddedItems)
                {
                    AddSourceFile(filePath, GetFolders(filePath, projectChange), context, isActiveContext);
                }

                foreach (KeyValuePair<string, string> filePaths in diff.RenamedItems)
                {
                    RemoveSourceFile(filePaths.Key, context);
                    AddSourceFile(filePaths.Value, GetFolders(filePaths.Value, projectChange), context, isActiveContext);
                }

                foreach (string filePath in diff.ChangedItems)
                {
                    // We add and then remove ChangedItems to handle Linked metadata changes
                    RemoveSourceFile(filePath, context);
                    AddSourceFile(filePath, GetFolders(filePath, projectChange), context, isActiveContext);
                }
            }
        }

        public override Task OnContextReleasedAsync(IWorkspaceProjectContext context)
        {
            _sourceFilesByContext.Remove(context);
            return Task.CompletedTask;
        }

        private void RemoveSourceFile(string filePath, IWorkspaceProjectContext context)
        {
            string fullPath = _project.MakeRooted(filePath);

            if (_sourceFilesByContext.TryGetValue(context, out HashSet<string> sourceFiles) && sourceFiles.Remove(fullPath))
            {
                context.RemoveSourceFile(fullPath);
            }
        }

        private void AddSourceFile(string filePath, string[] folderNames, IWorkspaceProjectContext context, bool isActiveContext)
        {
            string fullPath = _project.MakeRooted(filePath);

            if (!_sourceFilesByContext.TryGetValue(context, out HashSet<string> sourceFiles))
            {
                sourceFiles = new HashSet<string>(StringComparers.Paths);
                _sourceFilesByContext.Add(context, sourceFiles);
            }
            else if (sourceFiles.Contains(fullPath))
            {
                return;
            }

            sourceFiles.Add(fullPath);
            context.AddSourceFile(fullPath, folderNames: folderNames, isInCurrentContext: isActiveContext);
        }

        private string[] GetFolders(string filePath, IProjectChangeDescription projectChange)
        {
            // Roslyn wants the effective set of folders from the source up to, but not including the project 
            // root to handle the cases where linked files have a different path in the tree than what its path 
            // on disk is. It uses these folders for things that create files alongside others, such as extract
            // interface.

            return null;
        }
    }
}
