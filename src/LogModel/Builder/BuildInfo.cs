// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
