// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class SubTreeRootDependencyNode : DependencyNode
    {
        public SubTreeRootDependencyNode(string providerType,
                                         string caption,
                                         ProjectTreeFlags flags,
                                         ImageMoniker icon,
                                         ImageMoniker? expandedIcon = null,
                                         int priority = 0)

            : base(new DependencyNodeId(providerType, string.Empty, string.Empty), 
                   flags, priority, null, resolved:true)
        {
            Requires.NotNullOrEmpty(caption, nameof(caption));

            Caption = caption;
            Icon = icon;
            ExpandedIcon = expandedIcon.HasValue ? expandedIcon.Value : Icon;
        }
    }
}
