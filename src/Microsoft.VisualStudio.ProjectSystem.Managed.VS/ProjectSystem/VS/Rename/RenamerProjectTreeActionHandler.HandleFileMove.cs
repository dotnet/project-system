// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Settings;
using Solution = Microsoft.CodeAnalysis.Solution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal partial class RenamerProjectTreeActionHandler
    {
        private readonly HashSet<Renamer.RenameDocumentActionSet> _results = new HashSet<Renamer.RenameDocumentActionSet>();

        protected virtual async Task CPSCopyAsync(IProjectTreeActionHandlerContext context, IImmutableSet<IProjectTree> nodes, IProjectTree receiver, bool deleteOriginal = false)
        {
            await base.CopyAsync(context, nodes, receiver, deleteOriginal);
        }

        public async Task CopyAsync(IProjectTreeActionHandlerContext context, IImmutableSet<IProjectTree> nodes, IProjectTree receiver, bool deleteOriginal = false)
        {
            Requires.NotNull(context, nameof(Context));
            Requires.NotNull(nodes, nameof(nodes));
            Requires.NotNull(receiver, nameof(receiver));

            ISettingsManager settings = await _settingsManagerService.GetValueAsync();

            bool namespaceUpdateEnabled = settings.GetValueOrDefault("SolutionNavigator.EnableNamespaceUpdate", false);

            if (namespaceUpdateEnabled == false || receiver.IsFolder == false)
            {
                await CPSCopyAsync(context, nodes, receiver, deleteOriginal);
                return;
            }

            CodeAnalysis.Project? project = GetCurrentProject();
            if (project is null)
            {
                return;
            }

            // FilePath should not end with slashes
            string destinationFolder = Path.GetFileName(receiver.FilePath);

            _results.Clear();
            foreach (var node in nodes)
            {
                string oldFilePath = Path.GetFileNameWithoutExtension(node.FilePath);
                string filenameWithExtension = Path.GetFileName(node.FilePath);

                (bool result, Renamer.RenameDocumentActionSet? documentActions)
                    = await GetRenameSymbolsActions(project, oldFilePath, filenameWithExtension, new List<string>(new string[] { destinationFolder }));

                if (result == true)
                {
                    _results.Add(documentActions!);
                }
            }

            await CPSCopyAsync(context, nodes, receiver, deleteOriginal);

            foreach (var documentAction in _results)
            {
                _threadingService.RunAndForget(async () =>
                {
                    Solution currentSolution = await PublishLatestSolutionAsync();

                    await _projectVsServices.ThreadingService.SwitchToUIThread();

                    string actionMessage = documentAction.ApplicableActions.First().GetDescription(CultureInfo.CurrentCulture);

                    WaitIndicatorResult<Solution> result = _waitService.Run(
                        title: VSResources.Renaming_Type,
                        message: actionMessage,
                        allowCancel: true,
                        token => documentAction.UpdateSolutionAsync(currentSolution, token));

                    // Do not warn the user if the rename was cancelled by the user
                    if (result.IsCancelled)
                    {
                        return;
                    }

                    await _projectVsServices.ThreadingService.SwitchToUIThread();
                    _roslynServices.ApplyChangesToSolution(currentSolution.Workspace, result.Result);

                }, _unconfiguredProject);
            }
        }
    }
}
