// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IActiveConfiguredProjectProviderFactory
    {
        public static IActiveConfiguredProjectProvider ImplementActiveProjectConfiguration(Func<ProjectConfiguration> action)
        {
            var mock = new Mock<IActiveConfiguredProjectProvider>();
            mock.SetupGet(p => p.ActiveProjectConfiguration)
                .Returns(action);

            return mock.Object;
        }
    }
}
