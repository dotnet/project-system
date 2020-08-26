// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
