// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.ProjectSystem;

using Xunit;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Threading.Tasks
{

    [Trait("UnitTest", "ProjectSystem")]
    public class TaskDelaySchedulerTests
    {
        [Fact]
        public async Task ScheduleAsyncTask_RunsAsyncMethod()
        {
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(10), IProjectThreadingServiceFactory.Create(), CancellationToken.None);

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
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), CancellationToken.None);

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
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), CancellationToken.None);

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
        public async Task CancelPendingUpdates_SkipsPendingTasks()
        {
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(250), IProjectThreadingServiceFactory.Create(), CancellationToken.None);

            bool taskRan = false;
            var task = scheduler.ScheduleAsyncTask(ct =>
            {
                taskRan = true;
                return Task.CompletedTask;
            });

            scheduler.CancelPendingUpdates();

            await task;
            Assert.False(taskRan);
        }
    }
}
