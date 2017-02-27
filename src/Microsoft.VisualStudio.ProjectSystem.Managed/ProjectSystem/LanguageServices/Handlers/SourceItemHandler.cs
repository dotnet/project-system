// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
                AddSourceFile(sourceFile.Path, context, isActiveContext);
            }
        }

        public override async Task HandleAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e, IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext)
        {
            Requires.NotNull(e, nameof(e));
            Requires.NotNull(projectChange, nameof(projectChange));

            IProjectChangeDiff diff = projectChange.Difference;

            foreach (string filePath in diff.RemovedItems)
            {
                RemoveSourceFile(filePath, context);
            }

            if (diff.AddedItems.Count > 0 || diff.RenamedItems.Count > 0 || diff.ChangedItems.Count > 0)
            {
                // Make sure the tree matches the same version of the evaluation that we're handling for active project context.
                // If we are dealing with non- active project context, then just use the latest published tree.
                // TODO: https://github.com/dotnet/roslyn-project-system/issues/353
                IProjectTreeServiceState treeState = isActiveContext ?
                    await PublishTreeAsync(e).ConfigureAwait(true) :
                    await PublishLatestTreeAsync(e).ConfigureAwait(true);

                foreach (string filePath in diff.AddedItems)
                {
                    AddSourceFile(filePath, context, isActiveContext, treeState);
                }

                foreach (KeyValuePair<string, string> filePaths in diff.RenamedItems)
                {
                    RemoveSourceFile(filePaths.Key, context);
                    AddSourceFile(filePaths.Value, context, isActiveContext, treeState);
                }

                foreach (string filePath in diff.ChangedItems)
                {
                    // We add and then remove ChangedItems to handle Linked metadata changes
                    RemoveSourceFile(filePath, context);
                    AddSourceFile(filePath, context, isActiveContext);
                }
            }
        }

        public override Task OnContextReleasedAsync(IWorkspaceProjectContext context)
        {
            _sourceFilesByContext.Remove(context);
            return Task.CompletedTask;
        }

        private void RemoveSourceFile(string fullPath, IWorkspaceProjectContext context)
        {
            fullPath = _project.MakeRooted(fullPath);

            if (_sourceFilesByContext.TryGetValue(context, out HashSet<string> sourceFiles) && sourceFiles.Remove(fullPath))
            {
                context.RemoveSourceFile(fullPath);
            }
        }

        private void AddSourceFile(string fullPath, IWorkspaceProjectContext context, bool isActiveContext, IProjectTreeServiceState state = null)
        {
            fullPath = _project.MakeRooted(fullPath);

            if (!_sourceFilesByContext.TryGetValue(context, out HashSet<string> sourceFiles))
            {
                sourceFiles = new HashSet<string>(StringComparers.Paths);
                _sourceFilesByContext.Add(context, sourceFiles);
            }
            else if (sourceFiles.Contains(fullPath))
            {
                return;
            }

            string[] folderNames = Array.Empty<string>();
            if (state != null)  // We're looking at a generated file, which doesn't appear in a tree.
            {
                folderNames = GetFolders(fullPath, state).ToArray();
            }

            sourceFiles.Add(fullPath);
            context.AddSourceFile(fullPath, folderNames: folderNames, isInCurrentContext: isActiveContext);
        }

        private IEnumerable<string> GetFolders(string fullPath, IProjectTreeServiceState state)
        {
            // Roslyn wants the effective set of folders from the source up to, but not including the project 
            // root to handle the cases where linked files have a different path in the tree than what its path 
            // on disk is. It uses these folders for things that create files alongside others, such as extract
            // interface.

            IProjectTree tree = state.TreeProvider.FindByPath(state.Tree, fullPath);
            if (tree == null)
                yield break;    // We got a tree that is out-of-date than what our evaluation is based on

            IProjectTree parent = tree;

            do
            {
                // Source files can be nested under other files
                if (parent.IsFolder)
                    yield return parent.Caption;

                parent = parent.Parent;

            } while (!parent.IsRoot());
        }

        private Task<IProjectTreeServiceState> PublishTreeAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            try
            {
                // Make sure the tree matches the same version of the evaluation that we're handling
                return _projectTree.TreeService.PublishTreeAsync(e.ToRequirements(), blockDuringLoadingTree: true);
            }
            catch (ActiveProjectConfigurationChangedException)
            {
                // Project configuration changed while we blocked waiting for the tree, instead of trying to match
                // exactly the same version, just give up and publish the latest tree. Note, don't need to handle
                // ActiveProjectConfigurationChangedException as it will retry if the configuration changed.
                return PublishLatestTreeAsync(e);
            }
        }

        private Task<IProjectTreeServiceState> PublishLatestTreeAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> e)
        {
            return _projectTree.TreeService.PublishLatestTreeAsync(blockDuringLoadingTree: true);
        }
    }
}
