// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem
{
    // Mocks a System.IService provider to return Moqs of IVS* services.
    public class IServiceProviderMoq : SVsServiceProvider
    {
        // Usage. Create a new IServiceProviderMoq and add your service moqs to it.
        private Dictionary<Type, object> Services = new Dictionary<Type, object>();

        // Returns null if it can't get it
        public object GetService(Type serviceType)
        {
            Services.TryGetValue(serviceType, out object retVal);

            return retVal;
        }

        public void AddService(Type interfaceType, Type serviceType, object serviceMock)
        {
            Services.Add(serviceType, serviceMock);
            if (serviceType != interfaceType)
            {
                Services.Add(interfaceType, serviceMock);
            }
        }
    }
}
