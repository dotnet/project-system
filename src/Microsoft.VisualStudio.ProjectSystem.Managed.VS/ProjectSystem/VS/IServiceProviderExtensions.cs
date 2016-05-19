using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            InterfaceType service = null;

            try
            {
                service = sp.GetService(typeof(ServiceType)) as InterfaceType;
            }
            catch
            {
            }

            return service;
        }
    }
}

