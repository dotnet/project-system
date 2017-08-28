// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class DependencyViewModel : IDependencyViewModel
    {
        public string Caption { get; set; }
        public string FilePath { get; set; }
        public string SchemaName { get; set; }
        public string SchemaItemType { get; set; }
        public int Priority { get; set; }
        public ImageMoniker Icon { get; set; }
        public ImageMoniker ExpandedIcon { get; set; }
        public IImmutableDictionary<string, string> Properties { get; set; }
        public ProjectTreeFlags Flags { get; set; }
        public IDependency OriginalModel { get; set; }
    }
}
