using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    public class VBEntryPointProviderFake : Interop.IVBEntryPointProvider
    {
        public List<string> Fake_EntryPoints = new List<string>();
        #region IVBEntryPointProvider Members

        public VBEntryPointProviderFake()
        {
            Fake_EntryPoints.Add("Form1");
            Fake_EntryPoints.Add("Form2");
        }

        int Microsoft.VisualStudio.Editors.Interop.IVBEntryPointProvider.GetFormEntryPointsList(object pHierarchy, uint cItems, string[] bstrList, out uint pcActualItems)
        {
            if (bstrList == null || cItems < Fake_EntryPoints.Count)
            {
                pcActualItems = (uint)Fake_EntryPoints.Count;
                //UNDONE: force returning fewer than needed (they'd have to re-ask)

                return VSConstants.S_FALSE;
            }
            else 
            {
                pcActualItems = (uint)Fake_EntryPoints.Count;
                for (int i = 0; i < Fake_EntryPoints.Count; ++i)
                {
                    bstrList[i] = Fake_EntryPoints[i];
                }
                return VSConstants.S_OK;
            }
        }

        #endregion
    }
}
