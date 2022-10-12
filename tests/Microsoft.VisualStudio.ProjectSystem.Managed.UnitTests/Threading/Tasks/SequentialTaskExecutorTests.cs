// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public sealed class SequentialTaskExecutorTests : IDisposable
    {
        private readonly JoinableTaskContext _joinableTaskContext;

        public SequentialTaskExecutorTests()
        {
#pragma warning disable VSSDK005
            _joinableTaskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005
        }

        public void Dispose()
        {
            _joinableTaskContext.Dispose();
        }

        [Fact]
        public async Task EnsureTasksAreRunInOrder()
        {
            const int NumberOfTasks = 25;
            var sequencer = new SequentialTaskExecutor(new(_joinableTaskContext), "UnitTests");

            var tasks = new List<Task>();
            var sequences = new List<int>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                int num = i;
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    async Task func()
                    {
                        await Task.Delay(1);
                        sequences.Add(num);
                    }
                    await func();
                }));
            }

            await Task.WhenAll(tasks);
            for (int i = 0; i < NumberOfTasks; i++)
            {
                Assert.Equal(i, sequences[i]);
            }
        }

        [Fact]
        public async Task EnsureTasksAreRunInOrderWithReturnValues()
        {
            const int NumberOfTasks = 25;
            var sequencer = new SequentialTaskExecutor(new(_joinableTaskContext), "UnitTests");

            var tasks = new List<Task<int>>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                int num = i;
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    async Task<int> func()
                    {
                        await Task.Delay(1);
                        return num;
                    }
                    return await func();
                }));
            }

            await Task.WhenAll(tasks);
            for (int i = 0; i < NumberOfTasks; i++)
            {
                Assert.Equal(i, tasks[i].Result);
            }
        }

        [Fact]
        public async Task EnsureNestedCallsAreExecutedDirectly()
        {
            const int NumberOfTasks = 10;
            var sequencer = new SequentialTaskExecutor(new(_joinableTaskContext), "UnitTests");

            var tasks = new List<Task>();
            var sequences = new List<int>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                int num = i;
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    async Task func()
                    {
                        await sequencer.ExecuteTask(async () =>
                        {
                            await Task.Delay(1);
                            sequences.Add(num);
                        });
                    }
                    await func();
                }));
            }

            await Task.WhenAll(tasks);
            for (int i = 0; i < NumberOfTasks; i++)
            {
                Assert.Equal(i, sequences[i]);
            }
        }

        [Fact]
        public void CallToDisposedObjectShouldThrow()
        {
            var sequencer = new SequentialTaskExecutor(new(_joinableTaskContext), "UnitTests");
            sequencer.Dispose();
            Assert.ThrowsAsync<ObjectDisposedException>(() => sequencer.ExecuteTask(() => Task.CompletedTask));
        }

        [Fact]
        public async Task EnsureTasksCancelledWhenDisposed()
        {
            const int NumberOfTasks = 10;
            var sequencer = new SequentialTaskExecutor(new(_joinableTaskContext), "UnitTests");

            var tasks = new List<Task>();
            for (int i = 0; i < NumberOfTasks; i++)
            {
                tasks.Add(sequencer.ExecuteTask(async () =>
                {
                    static async Task func()
                    {
                        await Task.Delay(100);
                    }
                    await func();
                }));
            }
            sequencer.Dispose();

            bool mustBeCancelled = false;

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                for (int i = 0; i < NumberOfTasks; i++)
                {
                    // The first task or two may already be running. So we skip completed tasks until we find 
                    // one that is is cancelled
                    if (mustBeCancelled)
                    {
                        Assert.True(tasks[i].IsCanceled);
                    }
                    else
                    {
                        // All remaining tasks should be cancelled
                        mustBeCancelled = tasks[i].IsCanceled;
                    }
                }
            }

            Assert.True(mustBeCancelled);
        }
    }
}
