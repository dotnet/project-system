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
            Caption = Resources.FrameworkAssembliesNodeName;
            Icon = KnownMonikers.Library;
            ExpandedIcon = Icon;
            Priority = NuGetDependenciesSubTreeProvider.FrameworkAssemblyNodePriority;

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
