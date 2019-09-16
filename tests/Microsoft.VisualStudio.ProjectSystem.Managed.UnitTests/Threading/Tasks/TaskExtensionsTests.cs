// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    public class TaskExtensionsTests
    {
        [Fact]
        public async Task TaskExtensions_TryWaitForCompleteOrTimeoutTests()
        {
            var t1 = TaskResult.True;
            Assert.True(await t1.TryWaitForCompleteOrTimeout(1000));

            var t2 = Task.Delay(10000);
            Assert.False(await t2.TryWaitForCompleteOrTimeout(20));
        }
    }
}
