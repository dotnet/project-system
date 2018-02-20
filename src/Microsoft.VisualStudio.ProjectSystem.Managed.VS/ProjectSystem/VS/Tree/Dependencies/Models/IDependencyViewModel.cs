// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal interface IDependencyViewModel
    {
        string Caption { get; }
        string FilePath { get; }
        string SchemaName { get; }
        string SchemaItemType { get; }
        int Priority { get; }
        ImageMoniker Icon { get; }
        ImageMoniker ExpandedIcon { get; }
        IImmutableDictionary<string, string> Properties { get; }
        ProjectTreeFlags Flags { get; }
        IDependency OriginalModel { get; }

    }
}
