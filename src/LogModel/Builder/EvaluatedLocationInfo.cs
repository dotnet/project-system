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
        public TimeInfo Time { get; }

        public EvaluatedLocationInfo(string elementName, string elementDescription, EvaluationLocationKind kind, string file, int? line, TimeInfo time)
        {
            ElementName = elementName;
            ElementDescription = elementDescription;
            Kind = kind;
            File = file;
            Line = line;
            Time = time;
        }
    }
}
