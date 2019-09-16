// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.CSharp
{
    public class CSharpProjectCompatibilityProviderTests
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

        private static CSharpProjectCompatibilityProvider CreateInstance()
        {
            return new CSharpProjectCompatibilityProvider();
        }
    }
}
