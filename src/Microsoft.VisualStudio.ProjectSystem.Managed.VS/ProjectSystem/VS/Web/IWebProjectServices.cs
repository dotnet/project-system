// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Web.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    ///     Provides access to <see cref="IVsWebProjectContext"/> and 
    ///     its associated services.
    /// </summary>
    internal interface IWebProjectServices
    {
        IVsWebProjectContext Context
        {
            get;
        }

        TInterface GetContextService<TService, TInterface>() where TInterface : class;
    }
}
