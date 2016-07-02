// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageFrameworkAssembliesDependencyNode : DependencyNode
    {
        public PackageFrameworkAssembliesDependencyNode(
                                             DependencyNodeId id,
                                             string caption,
                                             ProjectTreeFlags flags,
                                             string parentItemSpec = null,
                                             IImmutableDictionary<string, string> properties = null,
                                             bool resolved = true)
            : base(id, flags, 0, properties, resolved)
        {
            Caption = caption;
            Icon = KnownMonikers.Library;
            ExpandedIcon = Icon;
            Priority = resolved
                            ? PackageDependencyNode.FrameworkAssemblyNodePriority
                            : PackageDependencyNode.UnresolvedReferenceNodePriority;

            // override flags here - exclude default Reference flags since they block graph nodes at the moment
            Flags = DependencyFlags
                        .Union(flags)
                        .Union(PreFilledFolderNode);
                        
        }

        public override string Alias
        {
            get
            {
                return Caption;
            }
        }
    }
}
