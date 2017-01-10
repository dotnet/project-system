// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class AnalyzerDependencyNode : DependencyNode
    {
        public AnalyzerDependencyNode(DependencyNodeId id,
                                      ProjectTreeFlags flags,
                                      int priority = 0,
                                      IImmutableDictionary<string, string> properties = null,
                                      bool resolved = true)
            : base(id, flags, priority, properties, resolved)
        {
            Requires.NotNullOrEmpty(id.ItemSpec, nameof(id.ItemSpec));

            if (resolved)
            {
                Icon = KnownMonikers.CodeInformation;

                Caption = Path.GetFileNameWithoutExtension(id.ItemSpec);
            }
            else
            {
                Icon = KnownMonikers.ReferenceWarning;

                // We show the full path that we couldn't resolve.
                Caption = id.ItemSpec;
            }

            Priority = AnalyzerNodePriority;
            ExpandedIcon = Icon;

            Flags = (resolved ? GenericResolvedDependencyFlags : GenericUnresolvedDependencyFlags)
                        .Union(flags);
        }
    }
}
