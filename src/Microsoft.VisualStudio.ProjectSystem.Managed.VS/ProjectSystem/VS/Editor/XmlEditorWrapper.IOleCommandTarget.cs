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

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal partial class XmlEditorWrapper : OnceInitializedOnceDisposed, IOleCommandTarget
    {
        private readonly WindowPane _delegatePane;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsProject _project;
        private readonly string _projectFileName;
        private IProjectThreadingService _threadingService;
        private ICollection<IProjectFileEditorCommandAsync> _commands;

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
            var componentModel = _serviceProvider.GetService<IComponentModel, SComponentModel>();

            var projectServiceAccessor = componentModel.GetService<IProjectServiceAccessor>();
            var projectService = projectServiceAccessor.GetProjectService();
            _threadingService = projectService.Services.ThreadingPolicy;

            var context = (IVsBrowseObjectContext)_project;

            // The IProjectFileEditorCommands live in the UnconfiguredProject scope, so we need to go through the unconfigured project
            // ExportProvider.
            var unconfiguredProject = context.UnconfiguredProject;
            _commands = unconfiguredProject.Services.ExportProvider.GetExportedValues<IProjectFileEditorCommandAsync>().ToList();
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            EnsureInitialized(true);
            foreach (var command in _commands)
            {
                if (command.CommandId == nCmdID)
                {
                    return _threadingService.ExecuteSynchronously(() => command.Handle(_project));
                }
            }
            return ((IOleCommandTarget)_delegatePane).Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) =>
            ((IOleCommandTarget)_delegatePane).QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

        protected override void Dispose(bool disposing)
        {
            if (disposing) _delegatePane.Dispose();
        }
    }
}
