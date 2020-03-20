// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs (wrapping IAsyncServiceProvider/SAsyncServiceProvider)

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsService{TInterfaceType, TServiceType}"/> that calls into Visual Studio's <see cref="SVsServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsService<,>))]
    internal class VsService<TService, TInterface> : VsService<TInterface>, IVsService<TService, TInterface>
        where TService : class
        where TInterface : class?
    {
        [ImportingConstructor]
        public VsService([Import(typeof(SAsyncServiceProvider))]IAsyncServiceProvider serviceProvider, JoinableTaskContext joinableTaskContext)
            : base(serviceProvider, joinableTaskContext)
        {
        }

        protected override Type ServiceType
        {
            get { return typeof(TService); }
        }
    }
}
