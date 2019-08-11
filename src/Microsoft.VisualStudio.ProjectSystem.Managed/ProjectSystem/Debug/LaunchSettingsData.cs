// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// This class is used to hold the data serialized from the json file.  
    /// </summary>
    internal class LaunchSettingsData
    {
        public Dictionary<string, object>? OtherSettings { get; set; }

        public List<LaunchProfileData>? Profiles { get; set; }
    }
}
