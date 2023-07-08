// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IServiceProviderExtensions
    {
        /// <summary>
        /// Returns the specified interface from the service. This is useful when the service and interface differ
        /// </summary>
        public static InterfaceType GetService<InterfaceType, ServiceType>(this IServiceProvider sp)
            where InterfaceType : class
            where ServiceType : class
        {
#pragma warning disable RS0030 // Do not used banned APIs
            return (InterfaceType)sp.GetService(typeof(ServiceType));
#pragma warning restore RS0030 // Do not used banned APIs
        }
    }
}
