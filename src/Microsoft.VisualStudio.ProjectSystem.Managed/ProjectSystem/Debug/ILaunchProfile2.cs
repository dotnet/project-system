// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Extends the settings of <see cref="ILaunchProfile"/>.
    /// </summary>
    internal interface ILaunchProfile2
    {
        bool RemoteDebugEnabled { get; }
        string? RemoteDebugMachine { get; }
    }
}
