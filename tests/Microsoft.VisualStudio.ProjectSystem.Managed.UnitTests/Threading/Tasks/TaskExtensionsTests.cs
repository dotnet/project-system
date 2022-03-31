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
        }
    }
}
