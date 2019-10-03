// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IUnconfiguredProjectTasksServiceFactory
    {
        public static IUnconfiguredProjectTasksService Create()
        {
            return Mock.Of<IUnconfiguredProjectTasksService>();
        }

        public static IUnconfiguredProjectTasksService ImplementPrioritizedProjectLoadedInHost(Func<Task> action)
        {
            var mock = new Mock<IUnconfiguredProjectTasksService>();

            mock.SetupGet(s => s.PrioritizedProjectLoadedInHost)
                .Returns(action);

            return mock.Object;
        }

        public static IUnconfiguredProjectTasksService CreateWithUnloadedProject<T>()
        {
            var mock = new Mock<IUnconfiguredProjectTasksService>();
            mock.Setup(t => t.LoadedProjectAsync(It.IsAny<Func<Task>>()))
                .Throws(new OperationCanceledException());

            mock.Setup(t => t.LoadedProjectAsync(It.IsAny<Func<Task<T>>>()))
                .Throws(new OperationCanceledException());

            var cancelledTask = Task.FromCanceled(new CancellationToken(canceled: true));

            mock.SetupGet(t => t.PrioritizedProjectLoadedInHost)
                .Returns(cancelledTask);

            mock.SetupGet(t => t.ProjectLoadedInHost)
                .Returns(cancelledTask);

            return mock.Object;
        }

        public static IUnconfiguredProjectTasksService ImplementLoadedProjectAsync(Func<Func<Task>, Task> action)
        {
            var mock = new Mock<IUnconfiguredProjectTasksService>();
            mock.Setup(t => t.LoadedProjectAsync(It.IsAny<Func<Task>>()))
                .Returns(action);

            return mock.Object;
        }

        public static IUnconfiguredProjectTasksService ImplementLoadedProjectAsync<T>(Func<Func<Task<T>>, Task<T>> action)
        {
            var mock = new Mock<IUnconfiguredProjectTasksService>();
            mock.Setup(t => t.LoadedProjectAsync(It.IsAny<Func<Task<T>>>()))
                .Returns(action);

            return mock.Object;
        }

        public static IUnconfiguredProjectTasksService ImplementUnloadCancellationToken(CancellationToken token)
        {
            var mock = new Mock<IUnconfiguredProjectTasksService>();
            mock.Setup(t => t.UnloadCancellationToken)
                .Returns(token);

            return mock.Object;
        }
    }
}
