using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class IEnumFake<T>
    {
        public List<T> Fake_EnumElements;
        private int Fake_CurrentIndex;

        public IEnumFake()
            : this(new T[] { })
        {
        }

        public IEnumFake(IEnumerable<T> enumElements)
            : this(enumElements, -1)
        {
        }

        protected IEnumFake(IEnumerable<T> enumElements, int index)
        {
            List<T> list = new List<T>(enumElements);
            Fake_EnumElements = list;
            Fake_CurrentIndex = index;
        }

        #region IEnum Members

        public int Clone()
        {
            throw new NotImplementedException();
        }

        public int Next(uint celt, T[] rgFrameworks, out uint pceltFetched)
        {
            uint[] celtFetched = new uint[] { 0 };
            int hr = Next(celt, rgFrameworks, celtFetched);
            pceltFetched = celtFetched[0];
            return hr;
        }

        public int Next(uint celt, T[] rgelt, uint[] pceltFetched)
        {
            pceltFetched[0] = (uint)Math.Min(celt, Fake_EnumElements.Count - Fake_CurrentIndex - 1);
            Array.Copy(Fake_EnumElements.ToArray(), Fake_CurrentIndex + 1, rgelt, 0, pceltFetched[0]);
            Fake_CurrentIndex += (int)pceltFetched[0];
            return VSConstants.S_OK;
        }

        public int Reset()
        {
            Fake_CurrentIndex = -1;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            Fake_CurrentIndex += (int)celt;
            return VSConstants.S_OK;
        }

        #endregion

    }
}

