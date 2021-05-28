// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class FaultExtensionTests
    {
        [Fact]
        public async Task RegisterFaultHandler_WhenBlockThrows_ReportsFault()
        {
            Exception result = null!;
            var faultHandler = IProjectFaultHandlerServiceFactory.ImplementHandleFaultAsync((ex, reportSettings, severity, project) => { result = ex; });
            var thrownException = new Exception();

            var block = DataflowBlockSlim.CreateActionBlock<string>(value =>
            {
                throw thrownException;
            });

            var faultTask = FaultExtensions.RegisterFaultHandlerAsync(faultHandler, block, null);

            await block.SendAsync("Hello");

            await faultTask;

            Assert.Equal(result.GetBaseException(), thrownException);
        }
    }
}
