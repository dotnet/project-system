// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
