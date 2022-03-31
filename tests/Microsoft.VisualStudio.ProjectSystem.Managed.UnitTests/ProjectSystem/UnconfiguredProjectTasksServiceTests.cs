// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
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
        public void SolutionLoadedInHost_SolutionServiceReturnsUncompletedTask_ReturnsUncompletedTask()
        {
            var solutionService = ISolutionServiceFactory.ImplementSolutionLoadedInHost(() => new Task(() => { }));

            var service = CreateInstance(solutionService: solutionService);

            var result = service.SolutionLoadedInHost;

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
        public void SolutionLoadedInHost_WhenProjectUnloaded_ReturnsCancelledTask()
        {
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(new CancellationToken(true));
            var service = CreateInstance(tasksService);

            var result = service.SolutionLoadedInHost;

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
        public void SolutionLoadedInHost_SolutionServiceSolutionReturnsCompletedTask_ReturnsCompletedTask()
        {
            var solutionService = ISolutionServiceFactory.ImplementSolutionLoadedInHost(() => Task.CompletedTask);

            var service = CreateInstance(solutionService: solutionService);

            var result = service.SolutionLoadedInHost;

            Assert.True(result.Status == TaskStatus.RanToCompletion);
        }

        [Fact]
        public void UnloadCancellationToken_WhenUnderlyingUnloadCancellationTokenCancelled_IsCancelled()
        {
            var source = new CancellationTokenSource();
            var tasksService = IProjectAsynchronousTasksServiceFactory.ImplementUnloadCancellationToken(source.Token);
            var service = CreateInstance(tasksService);

            Assert.False(service.UnloadCancellationToken.IsCancellationRequested);

            source.Cancel();

            Assert.True(service.UnloadCancellationToken.IsCancellationRequested);
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

        private static UnconfiguredProjectTasksService CreateInstance(IProjectAsynchronousTasksService? tasksService = null, ILoadedInHostListener? loadedInHostListener = null, ISolutionService? solutionService = null)
        {
            tasksService ??= IProjectAsynchronousTasksServiceFactory.Create();
            loadedInHostListener ??= ILoadedInHostListenerFactory.Create();
            solutionService ??= ISolutionServiceFactory.Create();

            return new UnconfiguredProjectTasksService(tasksService, IProjectThreadingServiceFactory.Create(), loadedInHostListener, solutionService);
        }
    }
}
