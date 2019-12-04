// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Workspace.VSIntegration.UI;

namespace Microsoft.VisualStudio.SolutionExplorer
{
    [ExportNodeExtender(CloudEnvironment.LiveShareSolutionView)]
    internal sealed class ProjectNodeExtender : INodeExtender
    {
        private readonly IWorkspaceCommandHandler _commandHandler;
        private static readonly string[] s_supportedProjectExtensions = new[] { ".csproj", ".vbproj" };

        [ImportingConstructor]
        public ProjectNodeExtender(
            JoinableTaskContext taskContext,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _commandHandler = new ProjectNodeExtenderCommandHandler(taskContext, serviceProvider);
        }

        public IChildrenSource? ProvideChildren(WorkspaceVisualNodeBase parentNode)
        {
            return null;
        }

        public IWorkspaceCommandHandler? ProvideCommandHandler(WorkspaceVisualNodeBase parentNode)
        {
            if (NodeRepresentsAManagedProject(parentNode))
            {
                return _commandHandler;
            }

            return null;
        }

        private static bool NodeRepresentsAManagedProject(WorkspaceVisualNodeBase node)
        {
            return node != null
                && node.VSSelectionMoniker != null
                && node.VSSelectionKind == CloudEnvironment.SolutionViewProjectGuid
                && node.NodeMoniker != null
                && s_supportedProjectExtensions.Any(extension => node.NodeMoniker.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
