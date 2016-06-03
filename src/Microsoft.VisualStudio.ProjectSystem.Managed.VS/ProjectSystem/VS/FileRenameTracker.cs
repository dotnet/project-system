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
        private readonly IVsEnvironmentServices _vsEnvironmentServices;

        [ImportingConstructor]
        public FileRenameTracker(IUnconfiguredProjectVsServices projectVsServices, VisualStudioWorkspace visualStudioWorkspace, IVsEnvironmentServices vsEnvironmentServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            Requires.NotNull(visualStudioWorkspace, nameof(visualStudioWorkspace));
            Requires.NotNull(vsEnvironmentServices, nameof(vsEnvironmentServices));

            _projectVsServices = projectVsServices;
            _visualStudioWorkspace = visualStudioWorkspace;
            _vsEnvironmentServices = vsEnvironmentServices;
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
            
            var renamer = new Renamer(_visualStudioWorkspace, _projectVsServices.ThreadingService, _vsEnvironmentServices, myProject, oldFilePath, newFilePath);
            _visualStudioWorkspace.WorkspaceChanged += renamer.OnWorkspaceChanged;
        }
    }
}
