// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public sealed class TaskResultTests
    {
        [Fact]
        public async Task True()
        {
            Assert.Same(TaskResult.True, TaskResult.True);

            Assert.True(await TaskResult.True);
        }

        [Fact]
        public async Task False()
        {
            Assert.Same(TaskResult.False, TaskResult.False);

            Assert.False(await TaskResult.False);
        }

        [Fact]
        public async Task Null()
        {
            Assert.Same(TaskResult.Null<string>(), TaskResult.Null<string>());
            Assert.NotSame(TaskResult.Null<string>(), TaskResult.Null<Array>());

            Assert.Null(await TaskResult.Null<string>());
        }
    }
}
