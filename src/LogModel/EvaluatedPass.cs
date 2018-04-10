// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class EvaluatedPass
    {
        public EvaluationPass Pass { get; }
        public ImmutableArray<EvaluatedLocation> Locations { get; }
        public TimeSpan ExclusiveTime { get; }
        public TimeSpan InclusiveTime { get; }
        public int NumberOfHits { get; }

        public EvaluatedPass(EvaluationPass pass, ImmutableArray<EvaluatedLocation> locations, TimeSpan exclusiveTime, TimeSpan inclusiveTime, int numberOfHits)
        {
            Pass = pass;
            Locations = locations;
            ExclusiveTime = exclusiveTime;
            InclusiveTime = inclusiveTime;
            NumberOfHits = numberOfHits;
        }
    }
}
