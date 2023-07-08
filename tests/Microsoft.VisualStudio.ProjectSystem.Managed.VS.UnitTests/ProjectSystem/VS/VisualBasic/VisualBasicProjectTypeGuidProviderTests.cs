// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.VisualBasic
{
    public class VisualBasicProjectTypeGuidProviderTests
    {
        [Fact]
        public void ProjectTypeGuid_ReturnsNonEmptyGuid()
        {
            var provider = CreateInstance();

            // Handshake between the project system and factory around the actual guid value so we do not test 
            // for a specified guid, other than to confirm it's not empty
            Assert.NotEqual(Guid.Empty, provider.ProjectTypeGuid);
        }

        private static VisualBasicProjectTypeGuidProvider CreateInstance()
        {
            return new VisualBasicProjectTypeGuidProvider();
        }
    }
}
