// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
