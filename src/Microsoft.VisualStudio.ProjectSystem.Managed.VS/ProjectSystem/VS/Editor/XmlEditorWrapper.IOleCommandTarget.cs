// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;

using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal partial class XmlEditorWrapper : OnceInitializedOnceDisposed, IOleCommandTarget
    {
        private readonly WindowPane _delegatePane;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsProject _project;

        private IProjectThreadingService _threadingService;
        private EditorStateModel _editorState;

        public XmlEditorWrapper(WindowPane delegatePane, IServiceProvider provider, IVsProject project)
        {
            Requires.NotNull(delegatePane, nameof(delegatePane));
            Requires.NotNull(provider, nameof(provider));
            Requires.NotNull(project, nameof(project));

            _delegatePane = delegatePane;
            _serviceProvider = provider;
            _project = project;
        }

        public void InitializeWindow() => EnsureInitialized(true);

        protected override void Initialize()
        {
            UIThreadHelper.VerifyOnUIThread();
            var componentModel = _serviceProvider.GetService<IComponentModel, SComponentModel>();

            var projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();
            var projectService = projectServiceAccessor.GetProjectService();
            _threadingService = projectService.Services.ThreadingPolicy;

            var context = (IVsBrowseObjectContext)_project;
            var unconfiguredProject = context.UnconfiguredProject;
            _editorState = unconfiguredProject.Services.ExportProvider.GetExportedValue<EditorStateModel>();
            _threadingService.ExecuteSynchronously(() => _editorState.InitializeTextBufferStateListenerAsync(_delegatePane));
        }

        public int Exec(ref Guid cmdGroupGuid, uint cmdId, uint cmdOptions, IntPtr pvaIn, IntPtr pvaOut)
        {
            EnsureInitialized(false);
            if (cmdId == VisualStudioStandard97CommandId.SaveProjectItem)
            {
                _threadingService.ExecuteSynchronously(_editorState.SaveProjectFileAsync);
                return VSConstants.S_OK;
            }
            return ((IOleCommandTarget)_delegatePane).Exec(ref cmdGroupGuid, cmdId, cmdOptions, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) =>
            ((IOleCommandTarget)_delegatePane).QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _delegatePane.Dispose();
            }
        }
    }
}
