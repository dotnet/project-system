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
        public void ScheduleAsyncTask_RunsAsyncMethod()
        {
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(10), new IProjectThreadingServiceMock(), CancellationToken.None);

            bool taskRan = false;
            var task = scheduler.ScheduleAsyncTask((ct) =>
            {
                taskRan = true;
                return Task.CompletedTask;
            });

            task.Task.Wait();
            Assert.True(taskRan);
        }

        [Fact]
        public void ScheduleAsyncTask_CancelsExistingTasks()
        {
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(50), new IProjectThreadingServiceMock(), CancellationToken.None);

            var tasksRun = new bool[3];
            var task1 = scheduler.ScheduleAsyncTask((ct) =>
            {
                tasksRun[0] = true;
                return Task.CompletedTask;
            });

            var task2 = scheduler.ScheduleAsyncTask((ct) =>
            {
                tasksRun[1] = true;
                return Task.CompletedTask;
            });

            var task3 = scheduler.ScheduleAsyncTask((ct) =>
            {
                tasksRun[2] = true;
                return Task.CompletedTask;
            });
            task1.Task.Wait();
            task2.Task.Wait();
            task3.Task.Wait();
            Assert.False(tasksRun[0]);
            Assert.False(tasksRun[1]);
            Assert.True(tasksRun[2]);
        }

        [Fact]
        public void Dispose_ClearsPendingTasks()
        {
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(50), new IProjectThreadingServiceMock(), CancellationToken.None);

            bool taskRan = false;
            var task1 = scheduler.ScheduleAsyncTask((ct) =>
            {
                taskRan = true;
                int count = 50;
                while (count != 0)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(20);
                    --count;
                }
                return Task.CompletedTask;
            });

            scheduler.Dispose();

            // There are two cases to consider in the verification. One, the cancellation is detected by the scheduler. In that case 
            // the task will complete successfully (it does not cancel itself), but the inner async task will not run.  And 2, the cancellation 
            // occurs after the scheduling starts. In that case the inner task will execute, but is crafted to throw and OperationCancelledException. 
            try
            {
                task1.Task.Wait();
                Assert.False(taskRan);
            }
            catch (OperationCanceledException)
            {
            }
        }

        [Fact]
        public void CancelPendingUpdates_PendingTasksAreCanceled()
        {
            var scheduler = new TaskDelayScheduler(TimeSpan.FromMilliseconds(50), new IProjectThreadingServiceMock(), CancellationToken.None);

            bool taskRan = false;
            var task1 = scheduler.ScheduleAsyncTask((ct) =>
            {
                taskRan = true;
                int count = 50;
                while (count != 0)
                {
                    ct.ThrowIfCancellationRequested();
                    Thread.Sleep(20);
                    --count;
                }
                return Task.CompletedTask;
            });

            scheduler.CancelPendingUpdates();

            try
            {
                task1.Task.Wait();
                Assert.False(taskRan);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
