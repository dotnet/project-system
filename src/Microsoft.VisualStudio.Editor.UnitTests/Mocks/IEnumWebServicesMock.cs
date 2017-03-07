using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.MockObjects;

using IVsWebService = Microsoft.VisualStudio.Shell.Interop.IVsWebService;
using IEnumWebServices = Microsoft.VisualStudio.Shell.Interop.IEnumWebServices;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class IEnumWebServicesMock : IEnumWebServices
    {
        private IVsWebService[] _webServices;
        private int _index;

        public IEnumWebServicesMock()
            : this(new IVsWebService[] { })
        {
        }

        public IEnumWebServicesMock(IEnumerable<IVsWebService> webServices)
            : this(webServices, -1)
        {
        }

        protected IEnumWebServicesMock(IEnumerable<IVsWebService> webServices, int index)
        {
            List<IVsWebService> websvcs = new List<IVsWebService>(webServices);
            _webServices = websvcs.ToArray();
            _index = index;
        }

        #region IEnumWebServices Members

        int IEnumWebServices.Clone(out IEnumWebServices ppenum)
        {
            ppenum = new IEnumWebServicesMock(_webServices, _index);
            return VSConstants.S_OK;
        }

        int IEnumWebServices.Next(uint celt, Microsoft.VisualStudio.Shell.Interop.IVsWebService[] rgelt, out uint pceltFetched)
        {
            pceltFetched = (uint)Math.Min(celt, _webServices.Length - _index - 1);
            Array.Copy(_webServices, _index + 1, rgelt, 0, pceltFetched);
            _index += (int)pceltFetched;
            return VSConstants.S_OK;
        }

        int IEnumWebServices.Reset()
        {
            _index = -1;
            return VSConstants.S_OK;
        }

        int IEnumWebServices.Skip(uint celt)
        {
            _index += (int)celt;
            return VSConstants.S_OK;
        }

        #endregion

    }
}
