// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsDelta
    {
        public readonly ImmutableArray<string> AllInputs;
        public readonly ImmutableArray<string> AllSharedInputs;
        public readonly ImmutableArray<DesignTimeInputFileChange> ChangedInputs;
        public readonly string OutputPath;

        public DesignTimeInputsDelta(IEnumerable<string> allInputs, IEnumerable<string> allSharedInputs, IEnumerable<DesignTimeInputFileChange> changedInputs, string outputPath)
        {
            AllInputs = ImmutableArray<string>.Empty.AddRange(allInputs);
            AllSharedInputs = ImmutableArray<string>.Empty.AddRange(allSharedInputs);
            ChangedInputs = ImmutableArray<DesignTimeInputFileChange>.Empty.AddRange(changedInputs);
            OutputPath = outputPath;
        }
    }
}
