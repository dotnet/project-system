// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Input;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands
{
    internal abstract class AbstractEditProjectFileCommand : AbstractSingleNodeProjectCommand
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectCapabilitiesService _projectCapabiltiesService;
        private readonly IServiceProvider _serviceProvider;

        public AbstractEditProjectFileCommand(IUnconfiguredProjectVsServices projectVsServices,
            IProjectCapabilitiesService projectCapabilitiesService,
            IServiceProvider serviceProvider)
        {
            _projectVsServices = projectVsServices;
            _projectCapabiltiesService = projectCapabilitiesService;
            _serviceProvider = serviceProvider;
        }

        protected override Task<CommandStatusResult> GetCommandStatusAsync(IProjectTree node, Boolean focused, String commandText, CommandStatus progressiveStatus) =>
            ShouldHandle(node) ?
                GetCommandStatusResult.Handled(GetCommandText(node), CommandStatus.Enabled) :
                GetCommandStatusResult.Unhandled;

        protected override async Task<Boolean> TryHandleCommandAsync(IProjectTree node, Boolean focused, Int64 commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
        {
            if (!ShouldHandle(node)) return false;

            await _projectVsServices.ThreadingService.SwitchToUIThread();
            var uiShellOpenDocument = _serviceProvider.GetService<IVsUIShellOpenDocument, SVsUIShellOpenDocument>();

            var runningDocTable = new RunningDocumentTable(_serviceProvider);
            var docData = Marshal.GetIUnknownForObject(runningDocTable.FindDocument(_projectVsServices.Project.FullPath));
            var oleAdapter = _serviceProvider.GetService<IOleServiceProvider>();

            // The solution needs to be the hierarchical parent for this. Because the docdata is the projectnode itself, using the projectnode as the
            // heirarchy parent would mean that the project node is its own parent.
            var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            Guid unusedGuid = Guid.Empty;
            IVsWindowFrame frame;

            var csprojPath = _projectVsServices.Project.FullPath;
            // This should never be able to throw, as this is getting the itemid of the solution. If this fails, we want to know about it.
            uint parentId = checked((uint)_projectVsServices.VsHierarchy.GetProperty<int>(Shell.VsHierarchyPropID.ParentHierarchyItemid));

            Verify.HResult(uiShellOpenDocument.OpenSpecificEditor(0,
                csprojPath,
                LoadedProjectFileEditorFactory.XmlEditorFactoryGuid,
                null,
                unusedGuid,
                "",
                solution as IVsUIHierarchy,
                parentId,
                docData,
                oleAdapter,
                out frame));

            frame.Show();

            return true;
        }

        protected abstract string GetCommandText(IProjectTree node);

        private bool ShouldHandle(IProjectTree node) => node.IsRoot() && _projectCapabiltiesService.Contains("OpenProjectFile");
    }
}
