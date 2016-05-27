//--------------------------------------------------------------------------------------------
// FileRenameTracker
//
// Exports an IProjectChangeHintReceiver to listen to file renames. If the file being renamed
// is a code file, it will prompt the user to rename the class to match. The rename is done
// using code model
//
// Copyright(c) 2015 Microsoft Corporation
//--------------------------------------------------------------------------------------------
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IProjectChangeHintReceiver)), Export]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityRenameHint.RenamedFileAsString)]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class FileRenameTracker : IProjectChangeHintReceiver
    {
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVsServices;
        private IComponentModel _componentModel;
        private VisualStudioWorkspace _visualStudioWorkspace;

        /// <summary>
        /// The thread handling service.
        /// </summary>
        [Import]
        private IProjectThreadingService ThreadingService { get; set; }

        /// <summary>
        /// Gets the VS global service provider.
        /// </summary>
        [Import]
        protected SVsServiceProvider ServiceProvider { get; private set; }

        [ImportingConstructor]
        public FileRenameTracker(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));
            _unconfiguredProjectVsServices = projectVsServices;
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
            return TplExtensions.CompletedTask;
        }

        private async Task ScheduleRenameAsync(string oldFilePath, string newFilePath)
        {
            string codeExtension = Path.GetExtension(newFilePath);
            if (codeExtension == null || !oldFilePath.EndsWith(codeExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await ThreadingService.SwitchToUIThread();

            if (_visualStudioWorkspace == null)
            {
                _componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
                _visualStudioWorkspace = _componentModel.GetService<VisualStudioWorkspace>();
            }

            IVsHierarchy hierarchy = _unconfiguredProjectVsServices.Hierarchy;

            EnvDTE.Project project = hierarchy.GetDTEProject();

            var myProject = _visualStudioWorkspace
                .CurrentSolution
                .Projects.Where(p => String.Equals(p.FilePath, project.FullName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            var renamer = new Renamer(_visualStudioWorkspace, ServiceProvider, ThreadingService, myProject, newFilePath, oldFilePath);
            _visualStudioWorkspace.WorkspaceChanged += renamer.OnWorkspaceChanged;
        }
    }
}
