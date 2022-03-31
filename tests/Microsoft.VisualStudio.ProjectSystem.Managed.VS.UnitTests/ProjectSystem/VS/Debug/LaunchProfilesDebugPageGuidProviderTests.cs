// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    public class LaunchProfilesDebugPageGuidProviderTests
    {
        [Fact]
        public async Task LaunchProfilesDebugPageGuidProvider_CheckGuid()
        {
            var guid = new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}");
            Assert.True(await new LaunchProfilesDebugPageGuidProvider().GetDebugPropertyPageGuidAsync() == guid);
        }
    }
}
