// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsUIService{TInterfaceType, TServiceType}"/> that calls into Visual Studio's <see cref="IServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsUIService<,>))]
    internal class VsUIService<TService, TInterface> : VsUIService<TInterface>, IVsUIService<TService, TInterface>
    {
        [ImportingConstructor]
        public VsUIService([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, IProjectThreadingService threadingService)
            : base(serviceProvider, threadingService)
        {
        }

        protected override Type ServiceType
        {
            get { return typeof(TService); }
        }
    }
}
