// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class ComDependencyNode : DependencyNode
    {
        public ComDependencyNode(DependencyNodeId id,
                                 ProjectTreeFlags flags,
                                 int priority = 0,
                                 IImmutableDictionary<string, string> properties = null,
                                 bool resolved = true)
            : base(id, flags, priority, properties, resolved)
        {
            Requires.NotNullOrEmpty(id.ItemSpec, nameof(id.ItemSpec));

            Caption = Path.GetFileNameWithoutExtension(id.ItemSpec);

            if (resolved)
            {
                Icon = KnownMonikers.Component;
            }
            else
            {
                Icon = KnownMonikers.ReferenceWarning;

                // We don't trim extension from here because usually there is no extension, and trimming it
                // would make "System.Xml" look like "System".
                Caption = Path.GetFileName(id.ItemSpec);
            }

            Priority = ComNodePriority;
            ExpandedIcon = Icon;
        }
    }
}
