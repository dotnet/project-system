using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal interface IVSRegistryHelper
    {
        IRegistryKey RegistryRoot(IServiceProvider serviceProvider, __VsLocalRegistryType registryType, bool writable);
    }

    [Export(typeof(IVSRegistryHelper))]
    internal class VSRegistryHelper : IVSRegistryHelper
    {
        public IRegistryKey RegistryRoot(IServiceProvider serviceProvider, __VsLocalRegistryType registryType, bool writable)
        {
            return new RegistryKeyWrapper(VSRegistry.RegistryRoot(serviceProvider, registryType, writable));
        }
    }
}
