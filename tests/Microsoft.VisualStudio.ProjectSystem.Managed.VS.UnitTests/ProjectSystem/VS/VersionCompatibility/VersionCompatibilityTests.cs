// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.VersionCompatibility
{
    public class VersionCompatibilityTests
    {
        [Fact]
        public void DataCorrectlySerializes()
        {
            string expectedSupportedMessage = "A newer version of Visual Studio is recommended for projects targetting .NET Core projects later then 2.2.";
            string versionDataString = $@" {{
  ""vsVersions"": {{
    ""15.6"": {{
      ""recommendedVersion"": ""2.0"",
      ""nonRecommendedVersionSelectedMessage"": """",
      ""supportedVersion"": ""2.1"",
      ""openSupportedMessage"": """",
      ""unsupportedVersion"": ""3.0"",
      ""unsupportedVersionsInstalledMessage"": """",
      ""openUnsupportedMessage"": """"
    }},
    ""15.8"": {{
      ""recommendedVersion"": ""2.1"",
      ""nonRecommendedVersionSelectedMessage"": ""{0} requires a newer version of Visual Studio."",
      ""supportedVersion"": ""2.2"",
      ""openSupportedMessage"": ""Visual Studio 2017 version 15.9 or newer is recommended for .NET Core 2.2 projects."",
      ""unsupportedVersion"": ""3.0"",
      ""unsupportedVersionsInstalledMessage"": """",
      ""openUnsupportedMessage"": """"
    }},
    ""15.9"": {{
      ""nonRecommendedVersionSelectedMessage"": """",
      ""supportedVersion"": ""2.3"",
      ""openSupportedMessage"": ""{expectedSupportedMessage}"",
      ""unsupportedVersion"": ""3.0"",
      ""unsupportedVersionsInstalledMessage"": """",
      ""openUnsupportedMessage"": """"
    }},
    ""16.0"": {{
      ""nonRecommendedVersionSelectedMessage"": """",
      ""supportedVersion"": ""3.0"",
      ""openSupportedMessage"": ""New string for 3.0 project open scenario"",
      ""unsupportedVersion"": ""3.1"",
      ""unsupportedVersionsInstalledMessage"": """",
      ""openUnsupportedMessage"": """"
    }}
  }}
}}";
            var data = VersionCompatibilityData.DeserializeVersionData(versionDataString);
            Assert.NotNull(data);
            Assert.False(data.TryGetValue(new Version("15.5"), out _));
            Assert.True(data.TryGetValue(new Version("15.6"), out _));
            Assert.True(data.TryGetValue(new Version("15.8"), out _));
            Assert.True(data.TryGetValue(new Version("16.0"), out _));
            Assert.True(data.TryGetValue(new Version("15.9"), out var compatibilityData));
            Assert.NotNull(compatibilityData.SupportedVersion);
            Assert.NotNull(compatibilityData.UnsupportedVersion);
            Assert.NotNull(compatibilityData.OpenSupportedMessage);
            Assert.NotNull(compatibilityData.OpenUnsupportedMessage);
            Assert.Equal(expectedSupportedMessage, compatibilityData.OpenSupportedMessage);
        }

        [Fact]
        public void DataCorrectlySerializes_PreviewVersion()
        {
            string expectedSupportedMessage = "A newer version of Visual Studio is recommended for projects targetting .NET Core projects later then 2.2.";
            string versionDataString = $@" {{
  ""vsVersions"": {{
    ""16.1"": {{
      ""openSupportedPreviewMessage"": ""{expectedSupportedMessage}"",
      ""supportedPreviewVersion"": ""3.0"",
    }}
  }}
}}";
            var data = VersionCompatibilityData.DeserializeVersionData(versionDataString);
            Assert.NotNull(data);
            Assert.True(data.TryGetValue(new Version("16.1"), out var compatibilityData));
            Assert.Null(compatibilityData.SupportedVersion);
            Assert.Null(compatibilityData.UnsupportedVersion);
            Assert.Null(compatibilityData.OpenSupportedMessage);
            Assert.Null(compatibilityData.OpenUnsupportedMessage);
            Assert.NotNull(compatibilityData.SupportedPreviewVersion);
            Assert.Equal(expectedSupportedMessage, compatibilityData.OpenSupportedPreviewMessage);
        }
    }
}
