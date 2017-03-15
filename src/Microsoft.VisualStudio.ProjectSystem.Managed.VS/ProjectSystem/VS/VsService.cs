// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsService{T}"/> that calls into Visual Studio's <see cref="SVsServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsService<,>))]
    internal class VsService<TInterfaceType, TServiceType> : IVsService<TInterfaceType, TServiceType>
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VsService([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));

            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public TInterfaceType Value
        {
            get
            {
                _threadingService.VerifyOnUIThread();

                TInterfaceType service = (TInterfaceType)_serviceProvider.GetService(typeof(TServiceType));

                Assumes.Present(service);

                return service;
            }
        }
    }

    [Export(typeof(IVsService<>))]
    internal class VsService<T> : VsService<T, T>
    {
        [ImportingConstructor]
        public VsService([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, IProjectThreadingService threadingService)
            : base(serviceProvider, threadingService)
        {
        }
    }
}
