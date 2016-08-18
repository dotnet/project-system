// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Interface definition for a mutable version of the launch settings snapshot.
    /// </summary>
    public interface IMutableLaunchSettings
    {

        /// <summary>
        /// Access to the current set of launch profiles
        /// </summary>
        List<IMutableLaunchProfile> Profiles { get; set; }

      
        /// <summary>
        /// Provides access to all the global settings
        /// </summary>
        Dictionary<string, object>  GlobalSettings { get;  set; }
    }
}
