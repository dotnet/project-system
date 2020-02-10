// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
