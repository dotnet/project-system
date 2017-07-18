// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ProjectSystem.VS;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.Shell
{
    // Adapts an IServiceProvider to an OLE IServiceProvider
    internal class ServiceProviderToOleServiceProviderAdapter : IOleServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderToOleServiceProviderAdapter(IServiceProvider serviceProvider)
        {
            Requires.NotNull(serviceProvider, "serviceProvider");

            _serviceProvider = serviceProvider;
        }

        public object ComServices { get; private set; }

        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;

            if (!TryGetService(guidService, out object service))
            {
                return HResult.NoInterface;
            }

            return GetComInterfaceForObject(service, riid, out ppvObject);
        }

        private bool TryGetService(Guid riid, out object service)
        {
            service = null;

            Type serviceType = Type.GetTypeFromCLSID(riid, throwOnError: true); // Should only throw on OOM according to MSDN

            service = _serviceProvider.GetService(serviceType);
            if (service == null)
                return false;

            return true;
        }

        private static HResult GetComInterfaceForObject(object instance, Guid iid, out IntPtr ppvObject)
        {
            Requires.NotNull(instance, "instance");

            IntPtr unknown = Marshal.GetIUnknownForObject(instance);
            if (iid.Equals(VSConstants.IID_IUnknown))
            {
                ppvObject = unknown;
                return HResult.OK;
            }

            HResult result = Marshal.QueryInterface(unknown, ref iid, out ppvObject);

            // Don't leak the IUnknown
            Marshal.Release(unknown);

            return result;
        }
    }
}
