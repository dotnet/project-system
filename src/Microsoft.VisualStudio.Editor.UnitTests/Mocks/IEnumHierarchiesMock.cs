// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.MockObjects;

using IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy;
using IEnumHierarchies = Microsoft.VisualStudio.Shell.Interop.IEnumHierarchies;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class IEnumHierarchiesMock : IEnumHierarchies
    {
        private IVsHierarchy[] _hiearchies;
        private int _index;

        public IEnumHierarchiesMock(IEnumerable<IVsHierarchy> hierarchies)
            : this(hierarchies, -1)
        {
        }

        protected IEnumHierarchiesMock(IEnumerable<IVsHierarchy> hierarchies, int index)
        {
            List<IVsHierarchy> hiers = new List<IVsHierarchy>(hierarchies);
            _hiearchies = hiers.ToArray();
            _index = index;
        }

        #region IEnumHierarchies Members

        int IEnumHierarchies.Clone(out IEnumHierarchies ppenum)
        {
            ppenum = new IEnumHierarchiesMock(_hiearchies, _index);
            return VSConstants.S_OK;
        }

        int IEnumHierarchies.Next(uint celt, IVsHierarchy[] rgelt, out uint pceltFetched)
        {
            pceltFetched = (uint) Math.Min(celt, _hiearchies.Length - _index - 1);
            Array.Copy(_hiearchies, _index + 1, rgelt, 0, pceltFetched);
            _index += (int) pceltFetched;
            return VSConstants.S_OK;
        }

        int IEnumHierarchies.Reset()
        {
            _index = -1;
            return VSConstants.S_OK;
        }

        int IEnumHierarchies.Skip(uint celt)
        {
            _index += (int) celt;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
