// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.COMAsyncServiceProvider.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.Shell.Interop
{
    internal static class IOleAsyncServiceProviderFactory
    {
        public static IOleAsyncServiceProvider ImplementQueryServiceAsync(object? service, Guid clsid)
        {
            var mock = new Mock<IOleAsyncServiceProvider>();

            mock.Setup(p => p.QueryServiceAsync(ref clsid))
              .Returns(IVsTaskFactory.FromResult(service));

            return mock.Object;
        }
    }
}
