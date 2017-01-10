// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class SdkDependencyNode : DependencyNode
    {
        public SdkDependencyNode(DependencyNodeId id,
                                 ProjectTreeFlags flags,
                                 int priority = 0,
                                 IImmutableDictionary<string, string> properties = null,
                                 bool resolved = true)
            : base(id, flags, priority, properties, resolved)
        {
            Requires.NotNullOrEmpty(id.ItemSpec, nameof(id.ItemSpec));

            Caption = id.ItemSpec.Split(CommonConstants.CommaDelimiter, StringSplitOptions.RemoveEmptyEntries)
                                 .FirstOrDefault();

            if (resolved)
            {
                Icon = KnownMonikers.BrowserSDK;
            }
            else
            {
                Icon = KnownMonikers.ReferenceWarning;
            }

            Priority = SdkNodePriority;
            ExpandedIcon = Icon;

            Flags = (resolved ? GenericResolvedDependencyFlags : GenericUnresolvedDependencyFlags)
                        .Union(flags);
        }
    }
}
