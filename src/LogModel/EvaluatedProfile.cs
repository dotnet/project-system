// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
