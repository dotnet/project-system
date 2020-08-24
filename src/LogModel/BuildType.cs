// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    [Flags]
    public enum BuildType
    {
        None = 0x0,
        Build = 0x1,
        DesignTimeBuild = 0x2,
        Evaluation = 0x4,
        Roslyn = 0x8,
        All = Build | DesignTimeBuild | Evaluation | Roslyn
    }
}
