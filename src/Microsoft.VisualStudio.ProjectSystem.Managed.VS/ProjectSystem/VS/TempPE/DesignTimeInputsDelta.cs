// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsDelta
    {
        public ImmutableHashSet<string> AllInputs { get; }
        public ImmutableHashSet<string> AllSharedInputs { get; }
        public ImmutableArray<DesignTimeInputFileChange> ChangedInputs { get; }
        public string OutputPath { get; }

        public DesignTimeInputsDelta(ImmutableHashSet<string> allInputs, ImmutableHashSet<string> allSharedInputs, IEnumerable<DesignTimeInputFileChange> changedInputs, string outputPath)
        {
            AllInputs = allInputs;
            AllSharedInputs = allSharedInputs;
            ChangedInputs = ImmutableArray.CreateRange(changedInputs);
            OutputPath = outputPath;
        }
    }
}
