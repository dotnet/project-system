// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfiguredProjectProviderFactory
    {
        public static IActiveConfiguredProjectProvider ImplementActiveProjectConfiguration(Func<ProjectConfiguration?> action)
        {
            var mock = new Mock<IActiveConfiguredProjectProvider>();
            mock.SetupGet<ProjectConfiguration?>(p => p.ActiveProjectConfiguration)
                .Returns(action);

            return mock.Object;
        }

        public static IActiveConfiguredProjectProvider Create(
            Func<ProjectConfiguration?>? getActiveProjectConfiguration = null,
            Func<ConfiguredProject?>? getActiveConfiguredProject = null)
        {
            var mock = new Mock<IActiveConfiguredProjectProvider>();
            mock.SetupGet<ProjectConfiguration?>(p => p.ActiveProjectConfiguration)
                .Returns(getActiveProjectConfiguration);

            mock.SetupGet<ConfiguredProject?>(p => p.ActiveConfiguredProject)
                .Returns(getActiveConfiguredProject);

            return mock.Object;
        }
    }
}
