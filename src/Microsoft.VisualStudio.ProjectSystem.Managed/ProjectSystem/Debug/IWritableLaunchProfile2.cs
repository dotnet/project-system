// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Extends the settings of <see cref="IWritableLaunchProfile"/>.
    /// </summary>
    internal interface IWritableLaunchProfile2
    {
        bool RemoteDebugEnabled { get; set; }
        string? RemoteDebugMachine { get; set; }
    }
}
