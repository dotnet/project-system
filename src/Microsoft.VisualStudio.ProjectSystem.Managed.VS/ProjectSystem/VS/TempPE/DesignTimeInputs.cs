// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputs
    {
        public ImmutableHashSet<string> Inputs { get; }
        public ImmutableHashSet<string> SharedInputs { get; }

        public DesignTimeInputs(IEnumerable<string> inputs, IEnumerable<string> sharedInputs)
        {
            Inputs = ImmutableHashSet.CreateRange(StringComparers.Paths, inputs);
            SharedInputs = ImmutableHashSet.CreateRange(StringComparers.Paths, sharedInputs);
        }
    }
}
