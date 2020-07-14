// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class EvaluatedProfile
    {
        public ImmutableArray<EvaluatedPass> Passes { get; }
        public Time EvaluationTime { get; }

        public EvaluatedProfile(ImmutableArray<EvaluatedPass> passes, Time evaluationTime)
        {
            Passes = passes;
            EvaluationTime = evaluationTime;
        }
    }
}
