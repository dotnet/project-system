// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using EmptyCollections = Microsoft.VisualStudio.ProjectSystem.Empty;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputsDelta
    {
        public ImmutableHashSet<string> Inputs { get; }
        public ImmutableHashSet<string> SharedInputs { get; }
        public ImmutableArray<DesignTimeInputFileChange> ChangedInputs { get; }
        public string TempPEOutputPath { get; }
        public static readonly  DesignTimeInputsDelta Empty = new DesignTimeInputsDelta(EmptyCollections.OrdinalStringSet,
            EmptyCollections.OrdinalStringSet, Enumerable.Empty<DesignTimeInputFileChange>(), string.Empty);

        public DesignTimeInputsDelta(ImmutableHashSet<string> inputs, ImmutableHashSet<string> sharedInputs, IEnumerable<DesignTimeInputFileChange> changedInputs, string tempPEOutputPath)
        {
            Inputs = inputs;
            SharedInputs = sharedInputs;
            ChangedInputs = ImmutableArray.CreateRange(changedInputs);
            TempPEOutputPath = tempPEOutputPath;
        }
    }
}
