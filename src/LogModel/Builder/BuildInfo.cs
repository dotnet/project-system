// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class BuildInfo : BaseInfo
    {
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public ImmutableDictionary<string, string> Environment { get; private set; }
        public Result Result { get; private set; }

        public void Start(DateTime startTime, ImmutableDictionary<string, string> environment)
        {
            StartTime = startTime;
            Environment = environment;
        }

        public void EndBuild(DateTime endTime, bool result)
        {
            EndTime = endTime;
            Result = result ? Result.Succeeded : Result.Failed;
        }
    }
}
