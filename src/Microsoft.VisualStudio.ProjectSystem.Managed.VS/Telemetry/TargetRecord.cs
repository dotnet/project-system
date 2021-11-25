// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Telemetry
{
    internal sealed class TargetRecord
    {
        public TargetRecord(string targetName, DateTime started)
        {
            TargetName = targetName;
            Started = started;
            Ended = DateTime.MinValue;
        }

        public string TargetName { get; }

        public DateTime Started { get; }

        public DateTime Ended { get; set; }

        public TimeSpan Elapsed => Ended - Started;
    }
}
