// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;

using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Linq;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Editor.Commands;
using System.Collections.Generic;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal partial class XmlEditorWrapper : OnceInitializedOnceDisposed, IOleCommandTarget
    {
        private readonly WindowPane _delegatePane;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsProject _project;
        //private readonly string _projectFileName;
        private IProjectThreadingService _threadingService;
        private ICollection<IProjectFileEditorCommandAsync> _commands;
        private IVsTextLines _textLines;
        private IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private IMsBuildAccessor _msbuildAccessor;
        private UnconfiguredProject _unconfiguredProject;
        private bool _lastDirtyState;

        public XmlEditorWrapper(WindowPane delegatePane, IServiceProvider provider, IVsProject project)
        {
            Requires.NotNull(delegatePane, nameof(delegatePane));
            Requires.NotNull(provider, nameof(provider));
            Requires.NotNull(project, nameof(project));

            _delegatePane = delegatePane;
            _serviceProvider = provider;
            _project = project;
        }

        protected override void Initialize()
        {
            UIThreadHelper.VerifyOnUIThread();
            var componentModel = _serviceProvider.GetService<IComponentModel, SComponentModel>();

            var projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();
            var projectService = projectServiceAccessor.GetProjectService();
            _threadingService = projectService.Services.ThreadingPolicy;

            _msbuildAccessor = projectService.Services.ExportProvider.GetExportedValue<IMsBuildAccessor>();

            var context = (IVsBrowseObjectContext)_project;

            // The IProjectFileEditorCommands live in the UnconfiguredProject scope, so we need to go through the unconfigured project
            // ExportProvider.
            _unconfiguredProject = context.UnconfiguredProject;
            _commands = _unconfiguredProject.Services.ExportProvider.GetExportedValues<IProjectFileEditorCommandAsync>().ToList();

            var bufferProvider = (IVsTextBufferProvider)_project;
            Verify.HResult(bufferProvider.GetTextBuffer(out _textLines));

            _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var buffer = _editorAdaptersFactoryService.GetDocumentBuffer(_textLines);
            buffer.Changed += Buffer_Changed;
        }

        private void Buffer_Changed(object sender, Text.TextContentChangedEventArgs e)
        {
            UIThreadHelper.VerifyOnUIThread();
            Verify.HResult(_textLines.GetStateFlags(out uint bufferStateFlags));


            // Checking whether the project is dirty involves acquiring a read lock, and is thus expensive to do on every change. Only check if the
            // document is not dirty itself
            var isDirty = ((BUFFERSTATEFLAGS)bufferStateFlags).IsDirty();
            if (!isDirty)
            {
                isDirty = GetProjectDirtyAsync();
            }

            if (isDirty != _lastDirtyState)
            {
                var windowFrame = _delegatePane.GetService<IVsWindowFrame, SVsWindowFrame>();
                windowFrame.SetProperty((int)__VSFPROPID2.VSFPROPID_OverrideDirtyState, isDirty ? 1 : 0);
                _lastDirtyState = isDirty;
            }
        }

        private bool GetProjectDirtyAsync()
        {
            return _threadingService.ExecuteSynchronously(() => _msbuildAccessor.IsProjectDirtyAsync(_unconfiguredProject));
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            EnsureInitialized(true);
            foreach (var command in _commands)
            {
                if (command.CommandId == nCmdID)
                {
                    return _threadingService.ExecuteSynchronously(() => command.HandleAsync(_project));
                }
            }
            return ((IOleCommandTarget)_delegatePane).Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) =>
            ((IOleCommandTarget)_delegatePane).QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var buffer = _editorAdaptersFactoryService.GetDocumentBuffer(_textLines);
                buffer.Changed -= Buffer_Changed;
                _delegatePane.Dispose();
            }
        }
    }

    internal static class BufferStateFlagsExtension
    {
        public static bool IsDirty(this BUFFERSTATEFLAGS flags)
        {
            return (flags & BUFFERSTATEFLAGS.BSF_MODIFIED) == BUFFERSTATEFLAGS.BSF_MODIFIED;
        }
    }
}
