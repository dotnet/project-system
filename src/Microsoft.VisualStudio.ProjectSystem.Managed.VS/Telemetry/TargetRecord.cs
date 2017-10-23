// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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
