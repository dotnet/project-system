// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
    /// <summary>
    /// Extends the Solution Explorer in cloud-connected scenarios by handling commands
    /// for nodes representing managed projects.
    /// </summary>
    internal class ProjectNodeCommandHandler : IWorkspaceCommandHandler
    {
        private readonly JoinableTaskContext _taskContext;
        private readonly IServiceProvider _serviceProvider;

        public ProjectNodeCommandHandler(JoinableTaskContext taskContext, IServiceProvider serviceProvider)
        {
            _taskContext = taskContext;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// The command handlers priority. If there are multiple handlers for a given node
        /// then they are called in order of decreasing priority.
        /// </summary>
        public int Priority => 2000;

        /// <summary>
        /// Whether or not this handler should be ignored when multiple nodes are selected.
        /// </summary>
        public bool IgnoreOnMultiselect => true;

        public int Exec(List<WorkspaceVisualNodeBase> selection, Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == CommandGroup.ManagedProjectSystemClientProjectCommandSetGuid)
            {
                int nCmdIDInt = (int)nCmdID;

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
                int nCmdIDInt = (int)nCmdID;

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

        /// <summary>
        /// Handles opening the file associated with the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node"></param>
        private void OpenFile(WorkspaceVisualNodeBase? node)
        {
            if (node == null
                || string.IsNullOrEmpty(node.NodeMoniker))
            {
                return;
            }

            _taskContext.Factory.RunAsync(async () =>
            {
                IBrokeredServiceContainer serviceContainer = _serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                ServiceHub.Framework.IServiceBroker serviceBroker = serviceContainer.GetFullAccessServiceBroker();

                IOpenDocumentService? openDocumentService = await serviceBroker.GetProxyAsync<IOpenDocumentService>(VisualStudioServices.VS2019_4.OpenDocumentService);

                try
                {
                    if (openDocumentService != null &&
                        node is IFileSystemNode fileSystemNode)
                        await openDocumentService.OpenDocumentAsync(fileSystemNode.FullPath, cancellationToken: default);
                    //else
                        // TODO: figure out what to tell the user if we can't get the service
                        // https://github.com/dotnet/project-system/issues/6306
                }
                finally
                {
                    (openDocumentService as IDisposable)?.Dispose();
                }
            });
        }
    }
}
