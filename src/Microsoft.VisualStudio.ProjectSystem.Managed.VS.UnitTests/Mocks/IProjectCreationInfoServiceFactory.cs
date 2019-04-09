// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem;

using Moq;

namespace Microsoft.VisualStudio
{
    internal static class IProjectCreationInfoServiceFactory
    {
        public static IProjectCreationInfoService Create(bool hasNewProjects = false)
        {
            var mock = new Mock<IProjectCreationInfoService>();
            mock.Setup(m => m.IsNewlyCreated(It.IsAny<UnconfiguredProject>())).Returns(hasNewProjects);
            return mock.Object;
        }
    }
}
