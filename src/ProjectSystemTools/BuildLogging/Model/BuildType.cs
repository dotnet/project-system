using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    [Flags]
    internal enum BuildType
    {
        None = 0x0,
        Build = 0x1,
        DesignTimeBuild = 0x2,
        Evaluation = 0x4
    }
}
