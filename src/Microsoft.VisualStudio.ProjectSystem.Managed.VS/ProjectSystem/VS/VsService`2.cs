// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsService{TInterfaceType, TServiceType}"/> that calls into Visual Studio's <see cref="SVsServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsService<,>))]
    internal class VsService<TService, TInterface> : VsService<TInterface>, IVsService<TService, TInterface>
    {
        [ImportingConstructor]
        public VsService([Import(typeof(SAsyncServiceProvider))]IAsyncServiceProvider serviceProvider, IProjectThreadingService threadingService)
            : base(serviceProvider, threadingService)
        {
        }

        protected override Type ServiceType
        {
            get { return typeof(TService); }
        }
    }
}
