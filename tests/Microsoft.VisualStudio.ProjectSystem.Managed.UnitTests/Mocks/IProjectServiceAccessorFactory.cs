// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;
using Moq;

namespace Microsoft.VisualStudio
{
    internal static class IProjectServiceAccessorFactory
    {
        public static IProjectServiceAccessor Create(IProjectCapabilitiesScope? scope = null, ConfiguredProject? configuredProject = null)
        {
            var mock = new Mock<IProjectServiceAccessor>();
            mock.Setup(s => s.GetProjectService(It.IsAny<ProjectServiceThreadingModel>()))
                .Returns(() => IProjectServiceFactory.Create(scope: scope, configuredProject: configuredProject));
            return mock.Object;
        }
    }
}
