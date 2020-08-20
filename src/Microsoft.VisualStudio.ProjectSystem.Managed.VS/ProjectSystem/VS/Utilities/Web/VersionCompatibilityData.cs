// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// Example data shown below. Support version is the version at which VS will warn the user and the warning will have a
    /// don't show again checkbox. The error version (if specified) indicates the version at which the customer is given a stronger
    /// message about support and does not have the don't show again dialog box option. The two versions establish a range of
    /// "supported" versions.
    /// eg:
    /// <code>
    /// {
    ///  "vsVersions": {
    ///    "15.6": {
    ///      "supportedVersion": "2.1",
    ///      "openSupportedMessage": "Warning when targeting 2.1",
    ///      "unsupportedVersion": "3.0",
    ///      "openUnsupportedMessage": "error when targeting 3.0 or newer"
    ///    }
    ///  }
    ///}
    ///</code>
    /// </summary>
    internal class VersionCompatibilityData
    {
        [JsonProperty(PropertyName = "supportedPreviewVersion")]
        public Version? SupportedPreviewVersion { get; set; }

        [JsonProperty(PropertyName = "openSupportedPreviewMessage")]
        public string? OpenSupportedPreviewMessage { get; set; }

        [JsonProperty(PropertyName = "supportedVersion")]
        public Version? SupportedVersion { get; set; }

        [JsonProperty(PropertyName = "openSupportedMessage")]
        public string? OpenSupportedMessage { get; set; }

        [JsonProperty(PropertyName = "unsupportedVersion")]
        public Version? UnsupportedVersion { get; set; }

        [JsonProperty(PropertyName = "openUnsupportedMessage")]
        public string? OpenUnsupportedMessage { get; set; }

        public static Dictionary<Version, VersionCompatibilityData> DeserializeVersionData(string versionDataString)
        {
            var vsVersionsObject = JObject.Parse(versionDataString);
            JToken vsVersions = vsVersionsObject.GetValue("vsVersions");
            return JsonConvert.DeserializeObject<Dictionary<Version, VersionCompatibilityData>>(vsVersions.ToString());
        }
    }
}
