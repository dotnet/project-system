// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
