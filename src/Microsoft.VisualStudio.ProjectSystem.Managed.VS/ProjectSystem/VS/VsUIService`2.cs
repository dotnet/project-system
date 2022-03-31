// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs (wrapping IServiceProvider)

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IVsUIService{TInterfaceType, TServiceType}"/> that calls into Visual Studio's <see cref="IServiceProvider"/>.
    /// </summary>
    [Export(typeof(IVsUIService<,>))]
    internal class VsUIService<TService, TInterface> : VsUIService<TInterface>, IVsUIService<TService, TInterface>
        where TService : class
        where TInterface : class?
    {
        [ImportingConstructor]
        public VsUIService([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider, JoinableTaskContext joinableTaskContext)
            : base(serviceProvider, joinableTaskContext)
        {
        }

        protected override Type ServiceType
        {
            get { return typeof(TService); }
        }
    }
}
