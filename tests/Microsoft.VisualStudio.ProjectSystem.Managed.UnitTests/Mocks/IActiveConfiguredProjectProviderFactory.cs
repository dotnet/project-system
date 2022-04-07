// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfiguredProjectProviderFactory
    {
        public static IActiveConfiguredProjectProvider ImplementActiveProjectConfiguration(Func<ProjectConfiguration?> action)
        {
            var mock = new Mock<IActiveConfiguredProjectProvider>();
            mock.SetupGet(p => p.ActiveProjectConfiguration)
                .Returns(action);

            return mock.Object;
        }

        public static IActiveConfiguredProjectProvider Create()
        {
            var mock = new Mock<IActiveConfiguredProjectProvider>();
            mock.SetupGet(p => p.ActiveConfiguredProjectBlock)
                .Returns(DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<ConfiguredProject>>());

            return mock.Object;
        }

        public static IActiveConfiguredProjectProvider Create(
            Func<ProjectConfiguration?>? getActiveProjectConfiguration = null,
            Func<ConfiguredProject?>? getActiveConfiguredProject = null)
        {
            var mock = new Mock<IActiveConfiguredProjectProvider>();
            mock.SetupGet(p => p.ActiveProjectConfiguration)
                .Returns(getActiveProjectConfiguration);

            mock.SetupGet(p => p.ActiveConfiguredProject)
                .Returns(getActiveConfiguredProject);

            return mock.Object;
        }
    }
}
