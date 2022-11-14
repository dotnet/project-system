// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IStartupProjectHelperFactory
    {
        internal static IStartupProjectHelper Create(ImmutableArray<string>? startProjectFullPaths = null)
        {
            var mock = new Mock<IStartupProjectHelper>();

            if (startProjectFullPaths.HasValue)
            {
                mock.Setup(t => t.GetFullPathsOfStartupProjects())
                    .Returns(startProjectFullPaths.Value);
            }

            return mock.Object;
        }
    }
}
