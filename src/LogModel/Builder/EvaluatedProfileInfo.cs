// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class EvaluatedProfileInfo
    {
        public ImmutableArray<EvaluatedPassInfo> Passes { get; }
        public TimeSpan ExclusiveEvaluationTime { get; }
        public TimeSpan InclusiveEvaluationTime { get; }
        public int NumberOfEvaluationHits { get; }
        public TimeSpan ExclusiveGlobTime { get; }
        public TimeSpan InclusiveGlobTime { get; }
        public int NumberOfGlobHits { get; }

        public EvaluatedProfileInfo(IEnumerable<EvaluatedPassInfo> passes, TimeSpan exclusiveEvaluationTime,
            TimeSpan inclusiveEvaluationTime, int numberOfEvaluationHits, TimeSpan exclusiveGlobTime,
            TimeSpan inclusiveGlobTime, int numberOfGlobHits)
        {
            Passes = passes.ToImmutableArray();
            ExclusiveEvaluationTime = exclusiveEvaluationTime;
            InclusiveEvaluationTime = inclusiveEvaluationTime;
            NumberOfEvaluationHits = numberOfEvaluationHits;
            ExclusiveGlobTime = exclusiveGlobTime;
            InclusiveGlobTime = inclusiveGlobTime;
            NumberOfGlobHits = numberOfGlobHits;
        }
    }
}
