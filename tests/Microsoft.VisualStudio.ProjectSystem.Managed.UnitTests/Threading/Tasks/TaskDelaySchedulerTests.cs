// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public class TaskDelaySchedulerTests
    {
        [Fact]
        public async Task ScheduleAsyncTask_RunsAsyncMethod()
        {
            using var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(10), IProjectThreadingServiceFactory.Create(), CancellationToken.None);
            bool taskRan = false;
            var task = scheduler.ScheduleAsyncTask(ct =>
            {
                taskRan = true;
                return Task.CompletedTask;
            });

            await task;

            Assert.True(taskRan);
        }

        [Fact]
        public async Task ScheduleAsyncTask_SkipsPendingTasks()
        {
            using var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), CancellationToken.None);
            var tasksRun = new bool[3];
            var task1 = scheduler.ScheduleAsyncTask(ct =>
            {
                tasksRun[0] = true;
                return Task.CompletedTask;
            });

            var task2 = scheduler.ScheduleAsyncTask(ct =>
            {
                tasksRun[1] = true;
                return Task.CompletedTask;
            });

            var task3 = scheduler.ScheduleAsyncTask(ct =>
            {
                tasksRun[2] = true;
                return Task.CompletedTask;
            });

            await task1;
            await task2;
            await task3;

            Assert.False(tasksRun[0]);
            Assert.False(tasksRun[1]);
            Assert.True(tasksRun[2]);
        }

        [Fact]
        public async Task Dispose_SkipsPendingTasks()
        {
            using var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), CancellationToken.None);
            bool taskRan = false;
            var task = scheduler.ScheduleAsyncTask(ct =>
            {
                taskRan = true;
                return Task.CompletedTask;
            });

            scheduler.Dispose();

            await task;
            Assert.False(taskRan);
        }

        [Fact]
        public async Task ScheduleAsyncTask_Noop_OriginalSourceTokenCancelled()
        {
            var cts = new CancellationTokenSource();
            using var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), cts.Token);
            cts.Cancel();

            bool taskRan = false;
            var task = scheduler.ScheduleAsyncTask(ct =>
            {
                taskRan = true;
                return Task.CompletedTask;
            });

            await task;
            Assert.False(taskRan);
        }

        [Fact]
        public async Task ScheduleAsyncTask_Noop_RequestTokenCancelled()
        {
            using var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), CancellationToken.None);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            bool taskRan = false;
            var task = scheduler.ScheduleAsyncTask(
                ct =>
                {
                    taskRan = true;
                    return Task.CompletedTask;
                },
                cts.Token);

            await task;
            Assert.False(taskRan);
        }
    }
}
