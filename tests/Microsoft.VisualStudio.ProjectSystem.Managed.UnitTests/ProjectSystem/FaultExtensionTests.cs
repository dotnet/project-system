// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public class FaultExtensionTests
    {
        [Fact]
        public async Task RegisterFaultHandler_WhenBlockThrows_ReportsFault()
        {
            Exception? result = null;
            var faultHandlerService = IProjectFaultHandlerServiceFactory.ImplementHandleFaultAsync((ex, reportSettings, severity, project) => { result = ex; });
            var thrownException = new Exception(message: "Test");

            var block = DataflowBlockSlim.CreateActionBlock<string>(value =>
            {
                throw thrownException;
            });

            var faultTask = faultHandlerService.RegisterFaultHandlerAsync(block, project: null);

            await block.SendAsync("Hello");

            await faultTask;

            Assert.NotNull(result);

            Assert.Equal(
                $"Project system data flow 'DataflowBlockSlim (ActionBlockSlimAsync`1 : {block.GetHashCode()})' closed because of an exception: Test.",
                result.Message);

            Assert.Same(thrownException, result.GetBaseException());
        }
    }
}
