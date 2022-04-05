// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
