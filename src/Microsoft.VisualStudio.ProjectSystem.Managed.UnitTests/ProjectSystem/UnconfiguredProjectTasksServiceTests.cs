// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Trait("UnitTest", "ProjectSystem")]
    public class UnconfiguredProjectTasksServiceTests
    {
        [Fact]
        public void ProjectLoadedInHost_WhenNotProjectLoadedInHost_ReturnsUncompletedTask()
        {
            var service = CreateInstance();

            var result = service.ProjectLoadedInHost;

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void PrioritizedProjectLoadedInHost_WhenNotPrioritizedProjectLoadedInHost_ReturnsUncompletedTask()
        {
            var service = CreateInstance();

            var result = service.PrioritizedProjectLoadedInHost;

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void ProjectLoadedInHost_WhenProjectUnloaded_ReturnsCancelledTask()
        {
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(new CancellationToken(true));
            var service = CreateInstance(tasksService);

            var result = service.ProjectLoadedInHost;

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void PrioritizedProjectLoadedInHost_WhenProjectUnloaded_ReturnsCancelledTask()
        {
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(new CancellationToken(true));
            var service = CreateInstance(tasksService);

            var result = service.PrioritizedProjectLoadedInHost;

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public void ProjectLoadedInHost_WhenPrioritizedProjectLoadedInHost_ReturnsUncompletedTask()
        {
            var service = CreateInstance();

            var result = service.ProjectLoadedInHost;

            service.OnPrioritizedProjectLoadedInHost();

            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void PrioritizedProjectLoadedInHost_WhenProjectLoadedInHost_ReturnsUncompletedTask()
        {
            var service = CreateInstance();

            var result = service.PrioritizedProjectLoadedInHost;

            service.OnProjectLoadedInHost();
            
            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void ProjectLoadedInHost_WhenProjectLoadedInHost_ReturnsCompletedTask()
        {
            var service = CreateInstance();

            var result = service.ProjectLoadedInHost;
            service.OnProjectLoadedInHost();

            Assert.True(result.Status == TaskStatus.RanToCompletion);
        }

        [Fact]
        public void PrioritizedProjectLoadedInHost_WhenPrioritizedProjectLoadedInHost_ReturnsCompletedTask()
        {
            var service = CreateInstance();

            var result = service.PrioritizedProjectLoadedInHost;
            service.OnPrioritizedProjectLoadedInHost();

            Assert.True(result.Status == TaskStatus.RanToCompletion);
        }

        [Fact]
        public void OnProjectFactoryCompleted_StartsLoadedInHostListening()
        {
            int callCount = 0;
            var loadedInHostListener = ILoadedInHostListenerFactory.ImplementStartListeningAsync(() => { callCount++; });

            var service = CreateInstance(loadedInHostListener: loadedInHostListener);

            service.OnProjectFactoryCompleted();

            Assert.Equal(1, callCount);
        }

        private static UnconfiguredProjectTasksService CreateInstance(IProjectAsynchronousTasksService tasksService = null, ILoadedInHostListener loadedInHostListener = null)
        {
            tasksService = tasksService ?? IProjectAsynchronousTasksServiceFactory.Create();
            loadedInHostListener  = loadedInHostListener ?? ILoadedInHostListenerFactory.Create();

            return new UnconfiguredProjectTasksService(tasksService, IProjectThreadingServiceFactory.Create(), loadedInHostListener);
        }
    }
}
