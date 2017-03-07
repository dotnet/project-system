using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.MockObjects;

using Microsoft.VisualStudio.Designer.Interfaces;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    public class IVSMDCodeDomProviderMock : IVSMDCodeDomProvider
    {
        private object provider;

        public IVSMDCodeDomProviderMock(object provider)
        {
            this.provider = provider;
        }

        #region IVSMDCodeDomProvider Members

        object IVSMDCodeDomProvider.CodeDomProvider
        {
            get { return provider; }
        }

        #endregion
    }
}
