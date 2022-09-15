// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            Requires.NotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
        }

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
            var serviceType = Type.GetTypeFromCLSID(riid, throwOnError: true); // Should only throw on OOM according to MSDN

#pragma warning disable RS0030 // Do not used banned APIs (deliberately adapting)
            service = _serviceProvider.GetService(serviceType);
#pragma warning restore RS0030 // Do not used banned APIs
            return service is not null;
        }

        private static HResult GetComInterfaceForObject(object instance, Guid iid, out IntPtr ppvObject)
        {
            Requires.NotNull(instance, nameof(instance));

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
