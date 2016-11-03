// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class PackageDependencyNode : DependencyNode
    {
        public PackageDependencyNode(DependencyNodeId id,
                                     string caption,
                                     ProjectTreeFlags flags,
                                     IImmutableDictionary<string, string> properties = null,
                                     bool resolved = true)
            : base(id, flags, 0, properties, resolved)
        {
            Requires.NotNullOrEmpty(caption, nameof(caption));

            Caption = caption;
            Icon = resolved ? KnownMonikers.PackageReference : KnownMonikers.ReferenceWarning;
            ExpandedIcon = Icon;
            Priority = resolved 
                            ? PackageNodePriority 
                            : UnresolvedReferenceNodePriority;

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
