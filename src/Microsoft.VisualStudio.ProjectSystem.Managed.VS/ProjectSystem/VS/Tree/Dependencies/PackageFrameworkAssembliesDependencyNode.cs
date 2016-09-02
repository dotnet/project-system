// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageFrameworkAssembliesDependencyNode : DependencyNode
    {
        public PackageFrameworkAssembliesDependencyNode(
                                             DependencyNodeId id,
                                             ProjectTreeFlags flags,
                                             IImmutableDictionary<string, string> properties = null,
                                             bool resolved = true)
            : base(id, flags, 0, properties, resolved)
        {
            Caption = VSResources.FrameworkAssembliesNodeName;
            Icon = KnownMonikers.Library;
            ExpandedIcon = Icon;
            Priority = NuGetDependenciesSubTreeProvider.FrameworkAssemblyNodePriority;

            // Note: PreFilledFolderNode flag suggests graph provider to assume that this node already 
            // has children added to it, so it can create graph nodes right away and not query them.
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
