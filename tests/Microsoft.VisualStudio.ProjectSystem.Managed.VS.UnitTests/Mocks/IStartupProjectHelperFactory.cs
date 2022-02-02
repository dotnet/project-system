// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands;
using Moq;

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
