// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Solution = Microsoft.CodeAnalysis.Solution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    internal partial class RenamerProjectTreeActionHandler
    {
        public async Task HintingAsync(IProjectChangeHint hint)
        {
            if (!(hint is IProjectChangeFileRenameHint renameHint))
            {
                return;
            }

            // Get the list of files that changed folder
            Dictionary<string, string> filesMoved = new Dictionary<string, string>();

            foreach (var renamedFile in renameHint.RenamedFiles)
            {
                string sourceDirectory = Path.GetDirectoryName(renamedFile.Key);
                string destinationDirectory = Path.GetDirectoryName(renamedFile.Value);
                if (sourceDirectory.Equals(destinationDirectory))
                {
                    continue;
                }

                filesMoved[renamedFile.Key] = Path.GetFileName(destinationDirectory);
            }

            CodeAnalysis.Project? project = GetCurrentProject();

            if (project is null)
            {
                return;
            }

            // Get the list of actions that will update the namespace
            _results.Clear();
            foreach (var fileMoved in filesMoved)
            {
                string filenameWithExtension = Path.GetFileName(fileMoved.Key);

                (bool result, Renamer.RenameDocumentActionSet? documentActions)
                    = await GetRenameSymbolsActions(project, fileMoved.Key, filenameWithExtension, new List<string>(new string[] { fileMoved.Value }));
                if (result == true)
                {
                    _results[fileMoved.Key] = documentActions!;
                }
            }
        }

        public Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {

            var tmp = from value in hints.Values
                      from hint in value
                      where hint is IProjectChangeFileRenameHint hint1
                      select (IProjectChangeFileRenameHint)hint;

            var fileMoveActions = from hint in tmp
                                  from renamedFile in hint.RenamedFiles
                                  where _results.ContainsKey(renamedFile.Key)
                                  select _results[renamedFile.Key];

            foreach (var documentAction in fileMoveActions)
            {

                _threadingService.RunAndForget(async () =>
                {
                    Solution currentSolution = await PublishLatestSolutionAsync();

                    await _projectVsServices.ThreadingService.SwitchToUIThread();

                    string updateNamespaceAction = documentAction.ApplicableActions.First().GetDescription(CultureInfo.CurrentCulture);

                    WaitIndicatorResult<Solution> result = _waitService.Run(
                        title: VSResources.Renaming_Type,
                        message: updateNamespaceAction,
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

            return Task.CompletedTask;
        }

    }
}
