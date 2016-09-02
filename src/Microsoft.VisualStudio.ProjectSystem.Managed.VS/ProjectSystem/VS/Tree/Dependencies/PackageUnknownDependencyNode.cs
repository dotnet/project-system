// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class PackageUnknownDependencyNode : DependencyNode
    {
        public PackageUnknownDependencyNode(
                                     DependencyNodeId id,
                                     string caption,
                                     ProjectTreeFlags flags,
                                     IImmutableDictionary<string, string> properties = null,
                                     bool resolved = true)
            : base(id, flags, NuGetDependenciesSubTreeProvider.UnresolvedReferenceNodePriority, 
                   properties, resolved)
        {
            Requires.NotNullOrEmpty(caption, nameof(caption));

            Caption = caption;
            Icon = KnownMonikers.QuestionMark;
            ExpandedIcon = Icon;

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
