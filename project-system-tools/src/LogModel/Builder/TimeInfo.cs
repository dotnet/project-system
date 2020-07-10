// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class TimeInfo
    {
        public TimeSpan ExclusiveTime { get; }
        public TimeSpan InclusiveTime { get; }

        public TimeInfo(TimeSpan exclusiveTime, TimeSpan inclusiveTime)
        {
            ExclusiveTime = exclusiveTime;
            InclusiveTime = inclusiveTime;
        }
    }
}
