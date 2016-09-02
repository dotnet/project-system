// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectServicesFactory
    {
        public static IUnconfiguredProjectServices Create(IProjectAsynchronousTasksService asyncTaskService = null)
        {
            var mock = new Mock<IUnconfiguredProjectServices>();

            if (asyncTaskService != null)
            {
                mock.Setup(s => s.ProjectAsynchronousTasks).Returns(asyncTaskService);
            }

            return mock.Object;
        }
    }
}
