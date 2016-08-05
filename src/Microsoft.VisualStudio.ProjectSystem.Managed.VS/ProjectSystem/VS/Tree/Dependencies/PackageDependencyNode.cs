// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    public class PackageDependencyNode : DependencyNode
    {
        public const int DiagnosticsNodePriority = 0; // for any custom nodes like errors or warnings
        public const int UnresolvedReferenceNodePriority = 1;
        public const int PackageNodePriority = 2;
        public const int FrameworkAssemblyNodePriority = 3;
        public const int PackageAssemblyNodePriority = 4;

        public PackageDependencyNode(DependencyNodeId id,
                                     string caption,
                                     ProjectTreeFlags flags,
                                     string parentItemSpec = null,
                                     IImmutableDictionary<string, string> properties = null,
                                     bool resolved = true)
            : base(id, flags, 0, properties, resolved)
        {
            Requires.NotNullOrEmpty(caption, nameof(caption));

            Caption = caption;
            Icon = resolved ? KnownMonikers.PackageReference : KnownMonikers.ReferenceWarning;
            ExpandedIcon = Icon;
            Priority = resolved 
                            ? PackageDependencyNode.PackageNodePriority 
                            : PackageDependencyNode.UnresolvedReferenceNodePriority;
            // override flags here - exclude default Reference flags since they block graph nodes at the moment
            Flags = (resolved ? ResolvedDependencyFlags : UnresolvedDependencyFlags)
                        .Add(ProjectTreeFlags.Common.ResolvedReference)
                        .Union(flags);
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
