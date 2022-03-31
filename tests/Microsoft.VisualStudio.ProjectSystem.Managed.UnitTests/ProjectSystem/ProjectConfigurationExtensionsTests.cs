// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    public sealed class ProjectConfigurationExtensionsTests
    {
        [Theory]
        [InlineData("Debug|AnyCPU")]
        [InlineData("Release|AnyCPU")]
        [InlineData("Debug|AnyCPU|net48")]
        [InlineData("Debug|AnyCPU|net6.0")]
        public void GetDisplayString(string pattern)
        {
            Assert.Equal(pattern, ProjectConfigurationFactory.Create(pattern).GetDisplayString());
        }
    }
}
