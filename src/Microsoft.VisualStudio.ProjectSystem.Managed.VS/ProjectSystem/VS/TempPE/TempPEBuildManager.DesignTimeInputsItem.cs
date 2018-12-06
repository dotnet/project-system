// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal partial class TempPEBuildManager
    {
        internal class DesignTimeInputsItem
        {
            public ImmutableHashSet<string> Inputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
            public ImmutableHashSet<string> SharedInputs { get; set; } = ImmutableHashSet.Create(StringComparers.Paths);
            public string OutputPath { get; internal set; }
        }
    }
}
