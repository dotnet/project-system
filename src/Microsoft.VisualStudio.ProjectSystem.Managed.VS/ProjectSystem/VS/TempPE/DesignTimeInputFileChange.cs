// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal class DesignTimeInputFileChange
    {
        public readonly string File;
        public readonly bool IgnoreFileWriteTime;

        public DesignTimeInputFileChange(string file, bool ignoreFileWriteTime)
        {
            File = file;
            IgnoreFileWriteTime = ignoreFileWriteTime;
        }
    }
}
