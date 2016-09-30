// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    [ProjectSystemTrait]
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task TaskExtensiosns_WaitForCompleteOrTimeoutTests()
        {
            var t1 = Task.FromResult(true);
            await t1.WaitForCompleteOrTimeout(1000);
            Assert.True(true);

            var t2 = Task.Delay(10000);
            try
            {
                await t2.WaitForCompleteOrTimeout(20);
                Assert.True(false);
            }
            catch (System.TimeoutException)
            {
            }
        }

        [Fact]
        public async Task TaskExtensiosns_TryWaitForCompleteOrTimeoutTests()
        {
            var t1 = Task.FromResult(true);
            Assert.True(await t1.TryWaitForCompleteOrTimeout(1000));

            var t2 = Task.Delay(10000);
            Assert.False(await t2.TryWaitForCompleteOrTimeout(20));
        }
    }
}
