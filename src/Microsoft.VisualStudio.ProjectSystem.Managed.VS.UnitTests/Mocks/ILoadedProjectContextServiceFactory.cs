// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class ILoadedProjectContextServiceFactory
    {
        public static ILoadedProjectContextService Create(Func<IProjectThreadingService> threadingServiceCreator = null)
        {
            var threadingService = threadingServiceCreator == null ? IProjectThreadingServiceFactory.Create() : threadingServiceCreator();

            var loadedProjectService = new Mock<ILoadedProjectContextService>();

            loadedProjectService
                .Setup(u => u.LoadedProjectAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> asyncAction) => threadingService.JoinableTaskFactory.RunAsync(asyncAction));

            return loadedProjectService.Object;
        }
    }
}
