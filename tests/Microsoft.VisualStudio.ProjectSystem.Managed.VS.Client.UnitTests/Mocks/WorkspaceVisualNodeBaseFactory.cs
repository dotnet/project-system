// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.Workspace.VSIntegration.UI
{
    internal static class WorkspaceVisualNodeBaseFactory
    {
        public static WorkspaceVisualNodeBase Implement(
            Guid? selectionKind = null,
            string? nodeMoniker = null,
            string? selectionMoniker = null)
        {
            return new TestWorkspaceVisualNode(
                INodeContainerFactory.Implement(),
                selectionKind ?? Guid.Empty,
                nodeMoniker ?? string.Empty,
                selectionMoniker ?? string.Empty);
        }

        private class TestWorkspaceVisualNode : WorkspaceVisualNodeBase
        {
            public TestWorkspaceVisualNode(INodeContainer container,
                Guid selectionKind,
                string nodeMoniker,
                string selectionMoniker)
                : base(container)
            {
                VSSelectionKind = selectionKind;
                NodeMoniker = nodeMoniker;
                VSSelectionMoniker = selectionMoniker;
            }
        }
    }
}
