// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using EmptyCollections = Microsoft.VisualStudio.ProjectSystem.Empty;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputSnapshot
    {
        public static readonly DesignTimeInputSnapshot Empty = new(
            EmptyCollections.OrdinalStringSet,
            EmptyCollections.OrdinalStringSet,
            Enumerable.Empty<DesignTimeInputFileChange>(), string.Empty);

        public DesignTimeInputSnapshot(ImmutableHashSet<string> inputs, ImmutableHashSet<string> sharedInputs, IEnumerable<DesignTimeInputFileChange> changedInputs, string tempPEOutputPath)
        {
            Inputs = inputs;
            SharedInputs = sharedInputs;
            ChangedInputs = ImmutableArray.CreateRange(changedInputs);
            TempPEOutputPath = tempPEOutputPath;
        }

        public ImmutableHashSet<string> Inputs { get; }

        public ImmutableHashSet<string> SharedInputs { get; }

        public ImmutableArray<DesignTimeInputFileChange> ChangedInputs { get; }

        public string TempPEOutputPath { get; }
    }
}
