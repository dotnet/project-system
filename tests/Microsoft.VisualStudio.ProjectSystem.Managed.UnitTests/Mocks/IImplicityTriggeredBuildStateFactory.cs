// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
    internal class IImplicityTriggeredBuildStateFactory
    {
        public static IImplicitlyTriggeredBuildState Create(bool implicitBuild, IEnumerable<string>? startupProjects = null)
        {
            var mock = new Mock<IImplicitlyTriggeredBuildState>();

            mock.SetupGet(state => state.IsImplicitlyTriggeredBuild)
                .Returns(implicitBuild);

            mock.SetupGet(state => state.StartupProjectFullPaths)
                .Returns(startupProjects?.ToImmutableArray() ?? ImmutableArray<string>.Empty);

            return mock.Object;
        }
    }
}
