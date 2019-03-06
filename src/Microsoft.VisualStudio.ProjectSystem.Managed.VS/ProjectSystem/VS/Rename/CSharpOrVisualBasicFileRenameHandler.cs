// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//--------------------------------------------------------------------------------------------
// <summary>
// CSharpOrVisualBasicFileRenameHandler
//
// Exports an IFileRenameHandler to listen to handle renames. If the file being renamed
// is a code file, it will prompt the user to rename the class to match. The rename is done
// using Roslyn Renamer API
// </summary>
//--------------------------------------------------------------------------------------------
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Rename;
using Microsoft.VisualStudio.ProjectSystem.Waiting;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Export(typeof(IFileRenameHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class CSharpOrVisualBasicFileRenameHandler : IFileRenameHandler
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IUnconfiguredProjectTasksService _unconfiguredProjectTasksService;
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IRenameTypeService _roslyn;
        private readonly IOperationWaitIndicator _waitService;

        [ImportingConstructor]
        public CSharpOrVisualBasicFileRenameHandler(IUnconfiguredProjectVsServices projectVsServices,
                                                    IUnconfiguredProjectTasksService unconfiguredProjectTasksService,
                                                    IEnvironmentOptions environmentOptions,
                                                    IUserNotificationServices userNotificationServices,
                                                    IRenameTypeService roslynServices,
                                                    IOperationWaitIndicator waitService)
        {
            _projectVsServices = projectVsServices;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _roslyn = roslynServices;
            _waitService = waitService;
        }

        public async Task HandleRenameAsync(string oldFilePath, string newFilePath)
        {
            // Do not offer to rename types if the user changes the file extensions
            if (!oldFilePath.EndsWith(Path.GetExtension(newFilePath), StringComparison.OrdinalIgnoreCase))
                return;

            // Check if there are any symbols that need to be renamed
            string projectPath = _projectVsServices.Project.FullPath;
            if (!await _roslyn.AnyTypeToRenameAsync(oldFilePath, newFilePath, projectPath))
                return;

            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            // Ask if the user wants to rename the symbol
            bool userConfirmed = await CheckUserConfirmationAsync(oldName);
            if (!userConfirmed)
                return;

            // Try and apply the changes to the current solution
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            bool renamedSolutionApplied = _waitService.WaitForAsyncOperation(
                title: VSResources.Rename,
                message: string.Format(CultureInfo.CurrentCulture, VSResources.Renaming_type_from_0_to_1, oldName, newName),
                allowCancel: true,
                token =>
                    // Do not let the project close until this completes
                    _unconfiguredProjectTasksService.LoadedProjectAsync(
                    () =>
                    {
                        // Perform the rename operation
                        return _roslyn.RenameTypeAsync(oldFilePath, newFilePath, projectPath, token);
                    }));


            // Notify the user if the rename could not be performed
            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                _userNotificationServices.ShowWarning(failureMessage);
            }

        }

        private async Task<bool> CheckUserConfirmationAsync(string oldFileName)
        {
            await _projectVsServices.ThreadingService.SwitchToUIThread();
            bool userNeedPrompt = _environmentOptions.GetOption("Environment", "ProjectsAndSolution", "PromptForRenameSymbol", false);
            if (userNeedPrompt)
            {
                string renamePromptMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolPrompt, oldFileName);
                return _userNotificationServices.Confirm(renamePromptMessage);
            }

            return true;
        }
    }
}
