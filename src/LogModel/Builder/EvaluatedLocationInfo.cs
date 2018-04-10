// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class EvaluatedLocationInfo
    {
        public string ElementName { get; }
        public string ElementDescription { get; }
        public EvaluationLocationKind Kind { get; }
        public string File { get; }
        public int? Line { get; }
        public TimeSpan ExclusiveTime { get; }
        public TimeSpan InclusiveTime { get; }
        public int NumberOfHits { get; }

        public EvaluatedLocationInfo(string elementName, string elementDescription, EvaluationLocationKind kind, string file, int? line, TimeSpan exclusiveTime, TimeSpan inclusiveTime, int numberOfHits)
        {
            ElementName = elementName;
            ElementDescription = elementDescription;
            Kind = kind;
            File = file;
            Line = line;
            ExclusiveTime = exclusiveTime;
            InclusiveTime = inclusiveTime;
            NumberOfHits = numberOfHits;
        }
    }
}
