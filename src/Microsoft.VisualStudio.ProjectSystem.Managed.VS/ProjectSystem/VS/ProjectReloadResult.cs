// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    // <summary>
    // Represents the result of an attempt to silently load the project
    // </summary>
    internal enum ProjectReloadResult
    {
        NoAction,
        ReloadCompleted,
        ReloadFailedProjectDirty,    // A complete reload of the project is required beccause the project is dirty in memory
        ReloadFailed,                // A complete reload of the project is required for some other reason - usually msbuild level reload failed
    }
}
