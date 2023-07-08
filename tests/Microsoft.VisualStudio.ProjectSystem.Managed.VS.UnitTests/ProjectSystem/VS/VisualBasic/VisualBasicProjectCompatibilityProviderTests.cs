// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem.VS.VisualBasic
{
    public class VisualBasicProjectCompatibilityProviderTests
    {
        [Fact]
        public async Task IsProjectCompatibleAsync_ReturnsTrue()
        {
            var provider = CreateInstance();

            var element = ProjectRootElement.Create();

            var result = await provider.IsProjectCompatibleAsync(element);

            Assert.True(result);
        }

        [Fact]
        public async Task IsProjectNeedBeUpgradedAsync_ReturnsFalse()
        {
            var provider = CreateInstance();

            var element = ProjectRootElement.Create();

            var result = await provider.IsProjectNeedBeUpgradedAsync(element);

            Assert.False(result);
        }

        private static VisualBasicProjectCompatibilityProvider CreateInstance()
        {
            return new VisualBasicProjectCompatibilityProvider();
        }
    }
}
