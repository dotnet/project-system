// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsDelta
    {
        public ImmutableHashSet<string> Inputs { get; }
        public ImmutableHashSet<string> SharedInputs { get; }
        public ImmutableArray<DesignTimeInputFileChange> ChangedInputs { get; }
        public string TempPEOutputPath { get; }

        public DesignTimeInputsDelta(ImmutableHashSet<string> inputs, ImmutableHashSet<string> sharedInputs, IEnumerable<DesignTimeInputFileChange> changedInputs, string tempPEOutputPath)
        {
            Inputs = inputs;
            SharedInputs = sharedInputs;
            ChangedInputs = ImmutableArray.CreateRange(changedInputs);
            TempPEOutputPath = tempPEOutputPath;
        }
    }
}
