// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.RpcContracts.OpenDocument;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

namespace Microsoft.VisualStudio.SolutionExplorer
{
    internal class ProjectNodeExtenderCommandHandler : IWorkspaceCommandHandler
    {
        private readonly JoinableTaskContext _taskContext;
        private readonly IServiceProvider _serviceProvider;

        public ProjectNodeExtenderCommandHandler(JoinableTaskContext taskContext, IServiceProvider serviceProvider)
        {
            _taskContext = taskContext;
            _serviceProvider = serviceProvider;
        }

        public int Priority => 2000;

        public bool IgnoreOnMultiselect => true;

        public int Exec(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid)
            {
                var nCmdIDInt = (int)nCmdID;

                switch (nCmdIDInt)
                {
                    case ManagedProjectSystemClientProjectCommandIds.EditProjectFile:
                        OpenFile(selection.SingleOrDefault());

                        return HResult.OK;
                }
            }

            return HResult.Ole.Cmd.NotSupported;
        }

        public bool QueryStatus(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, ref uint cmdf, ref string customTitle)
        {
            bool handled = false;

            if (pguidCmdGroup == CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid)
            {
                var nCmdIDInt = (int)nCmdID;

                switch (nCmdIDInt)
                {
                    case ManagedProjectSystemClientProjectCommandIds.EditProjectFile:
                        cmdf = (uint)(OLE.Interop.OLECMDF.OLECMDF_ENABLED | OLE.Interop.OLECMDF.OLECMDF_SUPPORTED);
                        handled = true;
                        break;
                }
            }

            return handled;
        }
        private void OpenFile(WorkspaceVisualNodeBase node)
        {
            if (node == null
                || string.IsNullOrEmpty(node.NodeMoniker))
            {
                return;
            }

            _taskContext.Factory.RunAsync(async () =>
            {
                var serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                var serviceBroker = serviceContainer.GetFullAccessServiceBroker();

                var openDocumentService = await serviceBroker.GetProxyAsync<IOpenDocumentService>(VisualStudioServices.VS2019_4.OpenDocumentService);

                try
                {
                    await openDocumentService.OpenDocumentAsync(node.NodeMoniker, cancellationToken: default);
                }
                finally
                {
                    (openDocumentService as IDisposable)?.Dispose();
                }
            });
        }
    }
}
