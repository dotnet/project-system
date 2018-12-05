// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal partial class TempPEBuildManager
    {
        internal class DesignTimeInputsDelta
        {
            public bool ShouldCompile { get; set; } = true;
            public ImmutableArray<string> AddedSharedItems { get; set; } = ImmutableArray<string>.Empty;
            public ImmutableArray<string> AddedItems { get; set; } = ImmutableArray<string>.Empty;
            public ImmutableArray<string> RemovedItems { get; set; } = ImmutableArray<string>.Empty;
            public IImmutableDictionary<NamedIdentity, IComparable> DataSourceVersions { get; set; } = ImmutableSortedDictionary<NamedIdentity, IComparable>.Empty;
            public string RootNamespace { get; internal set; }
            public string OutputPath { get; internal set; }
            public bool HasFileChanges => AddedSharedItems.Length > 0 || AddedItems.Length > 0 || RemovedItems.Length > 0;
        }
    }
}
