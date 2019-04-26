// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal partial class TempPEBuildManager
    {
        internal class DesignTimeInputsItem
        {
            public ImmutableHashSet<string> Inputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
            public ImmutableHashSet<string> SharedInputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
            public string OutputPath { get; set; }
            public ImmutableDictionary<string, uint> Cookies { get; set; } = ImmutableDictionary<string, uint>.Empty;
            public ImmutableDictionary<string, ITaskDelayScheduler> TaskSchedulers { get; set; } = ImmutableDictionary<string, ITaskDelayScheduler>.Empty;
        }
    }
}
