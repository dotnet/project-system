// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Time
    {
        public TimeSpan ExclusiveTime { get; }
        public TimeSpan InclusiveTime { get; }
        public int NumberOfHits { get; }

        public Time(TimeSpan exclusiveTime, TimeSpan inclusiveTime, int numberOfHits)
        {
            ExclusiveTime = exclusiveTime;
            InclusiveTime = inclusiveTime;
            NumberOfHits = numberOfHits;
        }
    }
}
