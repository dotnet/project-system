// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [DebuggerDisplay("{File}, ignore: {IgnoreFileWriteTime}")]
    internal class DesignTimeInputFileChange
    {
        public string File { get; }
        public bool IgnoreFileWriteTime { get; }

        public DesignTimeInputFileChange(string file, bool ignoreFileWriteTime)
        {
            File = file;
            IgnoreFileWriteTime = ignoreFileWriteTime;
        }
    }
}
