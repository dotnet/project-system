// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//--------------------------------------------------------------------------------------------
// <summary>
// FileRenameTracker
//
// Exports an IProjectChangeHintReceiver to listen to file renames. If the file being renamed
// is a code file, it will prompt the user to rename the class to match. The rename is done
// using Roslyn Renamer API
// </summary>
//--------------------------------------------------------------------------------------------
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectChangeHintReceiver))]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityRenameHint.RenamedFileAsString)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class FileRenameTracker : IProjectChangeHintReceiver
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IOptionsSettings _optionsSettings;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IRoslynServices _roslynServices;

        [ImportingConstructor]
        public FileRenameTracker(IUnconfiguredProjectVsServices projectVsServices, VisualStudioWorkspace visualStudioWorkspace, IOptionsSettings optionsSettings,  IUserNotificationServices userNotificationServices, IRoslynServices roslynServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(visualStudioWorkspace, nameof(visualStudioWorkspace));
            Requires.NotNull(optionsSettings, nameof(optionsSettings));
            Requires.NotNull(userNotificationServices, nameof(userNotificationServices));
            Requires.NotNull(roslynServices, nameof(roslynServices));

            _projectVsServices = projectVsServices;
            _visualStudioWorkspace = visualStudioWorkspace;
            _optionsSettings = optionsSettings;
            _userNotificationServices = userNotificationServices;
            _roslynServices = roslynServices;
        }

        public Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {
            var files = hints[ProjectChangeFileSystemEntityRenameHint.RenamedFile];
            if (files.Count == 1)
            {
                var hint = (IProjectChangeFileRenameHint)files.First();
                if (!hint.ChangeAlreadyOccurred)
                {
                    var kvp = hint.RenamedFiles.First();
                    ScheduleRenameAsync(kvp.Key, kvp.Value);
                }
            }

            return Task.CompletedTask;
        }

        public Task HintingAsync(IProjectChangeHint hint)
        {
            return Task.CompletedTask;
        }

        private void ScheduleRenameAsync(string oldFilePath, string newFilePath)
        {
            string codeExtension = Path.GetExtension(newFilePath);
            if (codeExtension == null || !oldFilePath.EndsWith(codeExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var myProject = _visualStudioWorkspace
                 .CurrentSolution
                 .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath))
                 .FirstOrDefault();
            
            var renamer = new Renamer(_visualStudioWorkspace, _projectVsServices.ThreadingService, _userNotificationServices,  _optionsSettings, _roslynServices,  myProject, oldFilePath, newFilePath);
            _visualStudioWorkspace.WorkspaceChanged += renamer.OnWorkspaceChangedAsync;
        }
    }
}
