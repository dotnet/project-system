// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable RS0030 // Do not used banned APIs (wrapping IServiceProvider)

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsUIService{T}"/> that calls into Visual Studio's <see cref="IServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsUIService<>))]
    internal class VsUIService<T> : IVsUIService<T> where T : class
    {
        private readonly Lazy<T> _value;
        private readonly JoinableTaskContext _joinableTaskContext;

        [ImportingConstructor]
        public VsUIService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, JoinableTaskContext joinableTaskContext)
        {
            Requires.NotNull(serviceProvider, nameof(serviceProvider));
            Requires.NotNull(joinableTaskContext, nameof(joinableTaskContext));

            _value = new Lazy<T>(() => (T)serviceProvider.GetService(ServiceType));
            _joinableTaskContext = joinableTaskContext;
        }

        public T? Value
        {
            get
            {
                // We always verify that we're on the UI thread regardless 
                // of whether we've already retrieved the service to always
                // enforce this.
                if (_joinableTaskContext.IsOnMainThread)
                {
                    return _value.Value;
                }

                throw new COMException("This method must be called on the UI thread.", HResult.WrongThread);
            }
        }

        protected virtual Type ServiceType
        {
            get { return typeof(T); }
        }
    }
}
