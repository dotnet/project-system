// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task TaskExtensions_TryWaitForCompleteOrTimeoutAsyncTests()
        {
            var t1 = TaskResult.True;
            Assert.True(await t1.TryWaitForCompleteOrTimeoutAsync(1000));

            var t2 = Task.Delay(10000);
            Assert.False(await t2.TryWaitForCompleteOrTimeoutAsync(20));

            var t3 = Task.Delay(20);
            Assert.True(await t3.TryWaitForCompleteOrTimeoutAsync(Timeout.Infinite));

            var t4 = Task.FromCanceled(new CancellationToken(canceled: true));
            await Assert.ThrowsAsync<TaskCanceledException>(() => t4.TryWaitForCompleteOrTimeoutAsync(1000));
            await Assert.ThrowsAsync<TaskCanceledException>(() => t4.TryWaitForCompleteOrTimeoutAsync(Timeout.Infinite));

            var ex = new Exception();
            var t5 = Task.FromException(ex);
            Assert.Same(ex, await Assert.ThrowsAsync<Exception>(() => t5.TryWaitForCompleteOrTimeoutAsync(1000)));
            Assert.Same(ex, await Assert.ThrowsAsync<Exception>(() => t5.TryWaitForCompleteOrTimeoutAsync(Timeout.Infinite)));
        }
    }
}
