// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Web.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    internal class WebProjectServices : IWebProjectServices
    {
        private static readonly Guid s_iID_IUnknown = new("00000000-0000-0000-C000-000000000046");
        private readonly IVsWebProjectContext _context;
        private readonly IProjectThreadingService _threadingService;

        public WebProjectServices(IProjectThreadingService threadingService, IVsWebProjectContext context)
        {
            _threadingService = threadingService;
            _context = context;
        }

        public IVsWebProjectContext Context
        {
            get
            {
                _threadingService.VerifyOnUIThread();

                return _context;
            }
        }

        public TInterface GetContextService<TService, TInterface>() where TInterface : class
        {
            _threadingService.VerifyOnUIThread();

            Guid serviceGuid = typeof(TService).GUID;
            Guid interfaceGuid = s_iID_IUnknown;
            
            // Not all interfaces requests that come through here are "COM-based", so we query IIUnknown first,
            // then do a cast to the inteface type, this will do a QI if COM-based interface, otherwise, a normal
            // managed cast.
            HResult result = _context.GetContextService(ref serviceGuid, ref interfaceGuid, out object service);
            if (result.Failed)
                throw result.Exception!;

            return (TInterface)service;
        }
    }
}
