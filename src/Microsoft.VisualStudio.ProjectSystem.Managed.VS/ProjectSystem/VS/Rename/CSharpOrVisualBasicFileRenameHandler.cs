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
        private readonly IRenameTypeService _renameTypeService;
        private readonly IOperationWaitIndicator _waitService;

        [ImportingConstructor]
        public CSharpOrVisualBasicFileRenameHandler(IUnconfiguredProjectVsServices projectVsServices,
                                                    IUnconfiguredProjectTasksService unconfiguredProjectTasksService,
                                                    IEnvironmentOptions environmentOptions,
                                                    IUserNotificationServices userNotificationServices,
                                                    IRenameTypeService renameTypeService,
                                                    IOperationWaitIndicator waitService)
        {
            _projectVsServices = projectVsServices;
            _unconfiguredProjectTasksService = unconfiguredProjectTasksService;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _renameTypeService = renameTypeService;
            _waitService = waitService;
        }

        public void HandleRename(string oldFilePath, string newFilePath)
            => _projectVsServices.ThreadingService.RunAndForget(
                () => HandleRenameAsync(oldFilePath, newFilePath), _projectVsServices.Project);


        public async Task HandleRenameAsync(string oldFilePath, string newFilePath)
        {
            // Do not offer to rename types if the user changes the file extensions
            if (!oldFilePath.EndsWith(Path.GetExtension(newFilePath), StringComparison.OrdinalIgnoreCase))
                return;

            // Check if there are any symbols that need to be renamed
            string projectPath = _projectVsServices.Project.FullPath;
            if (!await _renameTypeService.AnyTypeToRenameAsync(oldFilePath, newFilePath))
                return;

            string oldName = Path.GetFileNameWithoutExtension(oldFilePath);
            string newName = Path.GetFileNameWithoutExtension(newFilePath);

            // Try and apply the changes to the current solution
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            // Do not let the project close until this completes
            bool renamedSolutionApplied = await _unconfiguredProjectTasksService.LoadedProjectAsync(
             () =>
             {
                 // Perform the rename operation
                 return Task.FromResult(_renameTypeService.RenameType(oldFilePath, newFilePath, default));
             });


            // Notify the user if the rename could not be performed
            if (!renamedSolutionApplied)
            {
                string failureMessage = string.Format(CultureInfo.CurrentCulture, VSResources.RenameSymbolFailed, oldName);
                _userNotificationServices.ShowWarning(failureMessage);
            }
        }
    }
}
