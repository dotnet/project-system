// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public class VsSafeProjectGuidServiceTests
    {
        [Fact]
        public void GetProjectGuidAsync_WhenProjectAlreadyUnloaded_ReturnsCancelledTask()
        {
            var tasksService = IUnconfiguredProjectTasksServiceFactory.CreateWithUnloadedProject<string>();

            var accessor = CreateInstance(tasksService);

            var result = accessor.GetProjectGuidAsync();

            Assert.True(result.IsCanceled);
        }

        [Fact]
        public async Task GetProjectGuidAsync_WhenProjectUnloads_CancelsTask()
        {
            var projectUnloaded = new TaskCompletionSource();
            var tasksService = IUnconfiguredProjectTasksServiceFactory.ImplementPrioritizedProjectLoadedInHost(() => projectUnloaded.Task);

            var accessor = CreateInstance(tasksService);

            var result = accessor.GetProjectGuidAsync();

            Assert.False(result.IsCanceled);

            // Now "unload" the project
            projectUnloaded.SetCanceled();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            {
                return result;
            });
        }

        [Fact]
        public async Task GetProjectGuidAsync_WhenNoProjectGuidService_ReturnsEmpty()
        {
            var accessor = CreateInstance();

            var result = await accessor.GetProjectGuidAsync();

            Assert.Equal(result, Guid.Empty);
        }

        [Fact]
        public async Task GetProjectGuidAsync_WhenOnlyNonAsyncProjectGuidService_ReturnsProjectGuidProperty()
        {
            var accessor = CreateInstance();
            var guid = Guid.NewGuid();
            var projectGuidService = IProjectGuidServiceFactory.ImplementProjectGuid(guid);
            accessor.ProjectGuidServices.Add(projectGuidService);

            var result = await accessor.GetProjectGuidAsync();

            Assert.Equal(result, guid);
        }

        [Fact]
        public async Task GetProjectGuidAsync_WhenProjectGuidService2_ReturnsGetProjectGuidAsync()
        {
            var accessor = CreateInstance();
            var guid = Guid.NewGuid();
            var projectGuidService = IProjectGuidService2Factory.ImplementGetProjectGuidAsync(guid);
            accessor.ProjectGuidServices.Add(projectGuidService);

            var result = await accessor.GetProjectGuidAsync();

            Assert.Equal(result, guid);
        }

        [Fact]
        public async Task GetProjectGuidAsync_WhenProjectGuidService2Cancels_CancelsTask()
        {
            var accessor = CreateInstance();
            var projectGuidService = IProjectGuidService2Factory.ImplementGetProjectGuidAsync(() => throw new OperationCanceledException());
            accessor.ProjectGuidServices.Add(projectGuidService);

            var result = accessor.GetProjectGuidAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
            {
                return result;
            });
        }

        private static VsSafeProjectGuidService CreateInstance(IUnconfiguredProjectTasksService? tasksService = null)
        {
            var project = UnconfiguredProjectFactory.Create();
            tasksService ??= IUnconfiguredProjectTasksServiceFactory.ImplementPrioritizedProjectLoadedInHost(() => Task.CompletedTask);

            return new VsSafeProjectGuidService(project, tasksService);
        }
    }
}
