// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem
{
    // Mocks a System.IService provider to return Moqs of IVS* services.
    public class IAsyncServiceProviderMoq : IAsyncServiceProvider
    {
        // Usage. Create a new IAsyncServiceProviderMoq and add your service moqs to it.
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // Returns null if it can't get it
        public Task<object> GetServiceAsync(Type serviceType)
        {
            _services.TryGetValue(serviceType, out object retVal);

            return Task.FromResult(retVal);
        }

        public void AddService<T>(Type serviceType, T serviceMock)
        {
            _services.Add(serviceType, serviceMock);
        }
    }
}
