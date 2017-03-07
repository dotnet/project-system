using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class VBPackageWithDependencyInjection : VBPackage, IServiceProvider
    {
        private IServiceProvider _serviceProvider;
        public VBPackageWithDependencyInjection(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Initialize();
        }

        protected override void Initialize()
        {
            s_Instance = this;
        }
        #region IServiceProvider Members (re-implemented)

        object IServiceProvider.GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        #endregion
    }
}
