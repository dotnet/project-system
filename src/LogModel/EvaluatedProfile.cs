// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class EvaluatedProfile
    {
        public ImmutableArray<EvaluatedPass> Passes { get; }
        public TimeSpan ExclusiveEvaluationTime { get; }
        public TimeSpan InclusiveEvaluationTime { get; }
        public int NumberOfEvaluationHits { get; }
        public TimeSpan ExclusiveGlobTime { get; }
        public TimeSpan InclusiveGlobTime { get; }
        public int NumberOfGlobHits { get; }

        public EvaluatedProfile(ImmutableArray<EvaluatedPass> passes, TimeSpan exclusiveEvaluationTime,
            TimeSpan inclusiveEvaluationTime, int numberOfEvaluationHits, TimeSpan exclusiveGlobTime,
            TimeSpan inclusiveGlobTime, int numberOfGlobHits)
        {
            Passes = passes;
            ExclusiveEvaluationTime = exclusiveEvaluationTime;
            InclusiveEvaluationTime = inclusiveEvaluationTime;
            NumberOfEvaluationHits = numberOfEvaluationHits;
            ExclusiveGlobTime = exclusiveGlobTime;
            InclusiveGlobTime = inclusiveGlobTime;
            NumberOfGlobHits = numberOfGlobHits;
        }
    }
}
