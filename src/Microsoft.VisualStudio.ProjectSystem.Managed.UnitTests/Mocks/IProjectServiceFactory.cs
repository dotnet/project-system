// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectServiceFactory
    {
        public static IProjectService Create(ProjectServices services = null)
        {
            var mock = new Mock<IProjectService>();

            services = services ?? ProjectServicesFactory.Create(projectService: mock.Object);

            mock.Setup(p => p.Services)
                   .Returns(services);

            return mock.Object;
        }
    }
}
