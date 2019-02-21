// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//--------------------------------------------------------------------------------------------
// <summary>
// FileRenameTracker
//
// Exports an IFileRenameHandler to listen to handle renames. If the file being renamed
// is a code file, it will prompt the user to rename the class to match. The rename is done
// using Roslyn Renamer API
// </summary>
//--------------------------------------------------------------------------------------------
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Rename;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rename
{
    [Export(typeof(IFileRenameHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class FileRenameTracker : IFileRenameHandler
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IEnvironmentOptions _environmentOptions;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IRoslynServices _roslynServices;

        [ImportingConstructor]
        public FileRenameTracker(IUnconfiguredProjectVsServices projectVsServices,
                                 VisualStudioWorkspace visualStudioWorkspace,
                                 IEnvironmentOptions environmentOptions,
                                 IUserNotificationServices userNotificationServices,
                                 IRoslynServices roslynServices)
        {
            _projectVsServices = projectVsServices;
            _visualStudioWorkspace = visualStudioWorkspace;
            _environmentOptions = environmentOptions;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
        }

        public async ValueTask HandleRenameAsync(string oldFilePath, string newFilePath)
        {
            string codeExtension = Path.GetExtension(newFilePath);
            if (!oldFilePath.EndsWith(codeExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CodeAnalysis.Project myProject = _visualStudioWorkspace
                 .CurrentSolution
                 .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath))
                 .FirstOrDefault();

            if (myProject == null)
            {
                return;
            }

            var renamer = new Renamer(_visualStudioWorkspace, _projectVsServices.ThreadingService, _userNotificationServices, _environmentOptions, _roslynServices, myProject, oldFilePath, newFilePath);

            _visualStudioWorkspace.WorkspaceChanged += renamer.OnWorkspaceChangedAsync;
        }
    }
}
