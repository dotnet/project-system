// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal partial class DotNetCoreProjectCompatibilityDetector
    {
        internal enum CompatibilityLevel
        {
            Recommended = 0,
            Supported = 1,
            NotSupported = 2
        }
    }
}
