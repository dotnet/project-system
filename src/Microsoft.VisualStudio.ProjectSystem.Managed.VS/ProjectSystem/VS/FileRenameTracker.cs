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
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectChangeHintReceiver))]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityRenameHint.RenamedFileAsString)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class FileRenameTracker : IProjectChangeHintReceiver
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public FileRenameTracker(IUnconfiguredProjectVsServices projectVsServices, VisualStudioWorkspace visualStudioWorkspace, SVsServiceProvider serviceProvider)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(visualStudioWorkspace, nameof(VisualStudioWorkspace));
            Requires.NotNull(serviceProvider, nameof(SVsServiceProvider));
            _projectVsServices = projectVsServices;
            _visualStudioWorkspace = visualStudioWorkspace;
            _serviceProvider = serviceProvider;
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
                    await ScheduleRenameAsync(kvp.Key, kvp.Value).ConfigureAwait(false);
                }
            }
        }

        public Task HintingAsync(IProjectChangeHint hint)
        {
            return Task.CompletedTask;
        }

        private async Task ScheduleRenameAsync(string oldFilePath, string newFilePath)
        {
            string codeExtension = Path.GetExtension(newFilePath);
            if (codeExtension == null || !oldFilePath.EndsWith(codeExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _projectVsServices.ThreadingService.SwitchToUIThread();
            
            var myProject = _visualStudioWorkspace
                .CurrentSolution
                .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectVsServices.Project.FullPath))
                .FirstOrDefault();

            var renamer = new Renamer(_visualStudioWorkspace, _serviceProvider, _projectVsServices.ThreadingService, myProject, newFilePath, oldFilePath);
            _visualStudioWorkspace.WorkspaceChanged += renamer.OnWorkspaceChanged;
        }
    }
}
