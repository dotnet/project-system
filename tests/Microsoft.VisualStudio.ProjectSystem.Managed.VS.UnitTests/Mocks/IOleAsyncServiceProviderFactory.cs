// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;
using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;

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
