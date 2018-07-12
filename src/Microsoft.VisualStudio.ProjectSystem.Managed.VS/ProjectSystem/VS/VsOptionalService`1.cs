// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsOptionalService{T}"/> that calls into Visual Studio's <see cref="SVsServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsOptionalService<>))]
    internal class VsOptionalService<T> : IVsOptionalService<T>
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Type _serviceType;

        [ImportingConstructor]
        public VsOptionalService([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, IProjectThreadingService threadingService)
            : this(serviceProvider, threadingService, typeof(T))
        {
        }

        protected VsOptionalService(IServiceProvider serviceProvider, IProjectThreadingService threadingService, Type serviceType)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(serviceType, nameof(serviceType));

            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _serviceType = serviceType;
        }

        public T Value
        {
            get
            {
                _threadingService.VerifyOnUIThread();

                return (T)_serviceProvider.GetService(_serviceType);
            }
        }
    }
}
