// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class AssemblyDependencyNode : DependencyNode
    {
        public AssemblyDependencyNode(DependencyNodeId id,
                                      ProjectTreeFlags flags,
                                      int priority = 0,
                                      IImmutableDictionary<string, string> properties = null,
                                      bool resolved = true,
                                      string fusionName = null)
            : base(id, flags, priority, properties, resolved)
        {
            Requires.NotNullOrEmpty(id.ItemSpec, nameof(id.ItemSpec));

            if (resolved)
            {
                Icon = KnownMonikers.Reference;

                if (!string.IsNullOrEmpty(fusionName))
                {
                    var assemblyName = new AssemblyName(fusionName);
                    Caption = assemblyName.Name;
                }
                else
                {
                    Caption = Path.GetFileNameWithoutExtension(id.ItemSpec);
                }
            }
            else
            {
                Icon = KnownMonikers.ReferenceWarning;

                // We don't trim extension from here because usually there is no extension, and trimming it
                // would make "System.Xml" look like "System".
                Caption = Path.GetFileName(id.ItemSpec);
            }

            Priority = FrameworkAssemblyNodePriority;

            ExpandedIcon = Icon;
        }
    }
}
