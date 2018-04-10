// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class EvaluatedPassInfo
    {
        public EvaluationPass Pass { get; }
        public ImmutableArray<EvaluatedLocationInfo> Locations { get; }
        public TimeSpan ExclusiveTime { get; }
        public TimeSpan InclusiveTime { get; }
        public int NumberOfHits { get; }

        public EvaluatedPassInfo(EvaluationPass pass, IEnumerable<EvaluatedLocationInfo> locations, TimeSpan exclusiveTime, TimeSpan inclusiveTime, int numberOfHits)
        {
            Pass = pass;
            Locations = locations.ToImmutableArray();
            ExclusiveTime = exclusiveTime;
            InclusiveTime = inclusiveTime;
            NumberOfHits = numberOfHits;
        }
    }
}
