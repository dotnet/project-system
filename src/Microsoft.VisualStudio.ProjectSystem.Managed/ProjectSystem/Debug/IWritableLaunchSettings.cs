// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a modifiable launch settings
    /// </summary>
    public interface IWritableLaunchSettings
    {
        IWritableLaunchProfile? ActiveProfile { get; set; }

        List<IWritableLaunchProfile> Profiles { get; }

        Dictionary<string, object> GlobalSettings { get; }

        // Convert back to the immutable form
        ILaunchSettings ToLaunchSettings();
    }
}
