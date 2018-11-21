// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class VsServiceFactory
    {
        public static VsService<TService, TInterface> Create<TService, TInterface>(
            TInterface service = null,
            IAsyncServiceProvider serviceProvider = null,
            IProjectThreadingService threadingService = null) 
            where TInterface : class 
            where TService : class
        {
            return new MockService<TService, TInterface>(
                serviceProvider ?? IAsyncServiceProviderFactory.Create(),
                threadingService ?? IProjectThreadingServiceFactory.Create(),
                service);
        }

        private sealed class MockService<TService, TInterface> : VsService<TService, TInterface>
        {
            private readonly TInterface _service;

            public MockService(IAsyncServiceProvider serviceProvider, IProjectThreadingService threadingService, TInterface service)
                : base(serviceProvider, threadingService)
            {
                _service = service;
            }

            public override Task<TInterface> GetValueAsync()
            {
                return Task.FromResult(_service);
            }
        }
    }
}
