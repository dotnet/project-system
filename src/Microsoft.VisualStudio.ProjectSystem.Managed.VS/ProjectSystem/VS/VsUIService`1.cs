// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsUIService{T}"/> that calls into Visual Studio's <see cref="IServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsUIService<>))]
    internal class VsUIService<T> : IVsUIService<T>
    {
        private readonly Lazy<T> _value;
        private readonly IProjectThreadingService _threadingService;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VsUIService([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, IProjectThreadingService threadingService)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));

            _value = new Lazy<T>(GetService);
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
        }

        public T Value
        {
            get
            {
                // We always verify that we're on the UI thread regardless 
                // of whether we've already retrieved the service to always
                // enforce this.
                _threadingService.VerifyOnUIThread();

                return _value.Value;
            }
        }

        protected virtual Type ServiceType
        {
            get { return typeof(T); }
        }

        protected virtual T GetService()
        {
            return (T)_serviceProvider.GetService(ServiceType);
        }
    }
}
