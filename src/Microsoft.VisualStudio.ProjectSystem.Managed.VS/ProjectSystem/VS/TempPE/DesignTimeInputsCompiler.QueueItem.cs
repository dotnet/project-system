// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal partial class DesignTimeInputsCompiler
    {
        [DebuggerDisplay("{FileName}")]
        public class QueueItem
        {
            public string FileName { get; }
            public string TempPEOutputPath { get; }
            public bool IgnoreFileWriteTime { get; }
            public ImmutableHashSet<string> SharedInputs { get; }

            public QueueItem(string fileName, ImmutableHashSet<string> sharedInputs, string tempPEOutputPath, bool ignoreFileWriteTime)
            {
                FileName = fileName;
                SharedInputs = sharedInputs;
                TempPEOutputPath = tempPEOutputPath;
                IgnoreFileWriteTime = ignoreFileWriteTime;
            }
        }
    }
}
