using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
//using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    struct SupportedFrameworkFake
    {
        public uint version;
        public string description;

        public SupportedFrameworkFake(uint version, string description)
        {
            this.version = version;
            this.description = description;
        }
    }

    class VsTargetFrameworkAssembliesFake : IVsTargetFrameworkAssemblies
    {
        public List<SupportedFrameworkFake> Fake_SupportedFrameworks = new List<SupportedFrameworkFake>();

        public void Fake_AddVersions1_2_3()
        {
            Fake_SupportedFrameworks.Add(new SupportedFrameworkFake(1u, "Version 1"));
            Fake_SupportedFrameworks.Add(new SupportedFrameworkFake(2u, "Version 2"));
            Fake_SupportedFrameworks.Add(new SupportedFrameworkFake(3u, "Version 3"));
        }

        #region IVsTargetFrameworkAssemblies Members

        int IVsTargetFrameworkAssemblies.GetRequiredTargetFrameworkVersion(string szAssemblyFile, out uint pTargetFrameworkVersion)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsTargetFrameworkAssemblies.GetRequiredTargetFrameworkVersionFromDependency(string szAssemblyFile, out uint pTargetFrameworkVersion)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsTargetFrameworkAssemblies.GetSupportedFrameworks(out IEnumTargetFrameworks pTargetFrameworks)
        {
            List<uint> supportedFrameworks = new List<uint>();
            foreach (SupportedFrameworkFake fwk in Fake_SupportedFrameworks)
            {
                supportedFrameworks.Add(fwk.version);
            }

            pTargetFrameworks = new EnumTargetFrameworksFake(supportedFrameworks.ToArray());
            return VSConstants.S_OK;
        }

        int IVsTargetFrameworkAssemblies.GetSystemAssemblies(uint targetVersion, out IEnumSystemAssemblies pAssemblies)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsTargetFrameworkAssemblies.GetTargetFrameworkDescription(uint targetVersion, out string pszDescription)
        {
            pszDescription = "ERROR!!!! Shouldn't be using the return string if it didn't succeed";

            foreach (SupportedFrameworkFake fwk in Fake_SupportedFrameworks)
            {
                if (fwk.version == targetVersion)
                {
                    pszDescription = fwk.description;
                    return VSConstants.S_OK;
                }
            }

            return VSConstants.E_INVALIDARG;
        }

        int IVsTargetFrameworkAssemblies.IsSystemAssembly(string szAssemblyFile, out int pIsSystem, out uint pTargetFrameworkVersion)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }


    class EnumTargetFrameworksFake : IEnumTargetFrameworks
    {
        private IEnumFake<uint> m_enum;

        public EnumTargetFrameworksFake(uint[] supportedFrameworkVersions)
        {
            m_enum = new IEnumFake<uint>(supportedFrameworkVersions);
        }

        #region IEnumTargetFrameworks Members

        int IEnumTargetFrameworks.Clone(out IEnumTargetFrameworks ppIEnumComponents)
        {
            throw new NotImplementedException();
        }

        int IEnumTargetFrameworks.Count(out uint pCount)
        {
            pCount = (uint)m_enum.Fake_EnumElements.Count;
            return VSConstants.S_OK;
        }

        int IEnumTargetFrameworks.Next(uint celt, uint[] rgFrameworks, out uint pceltFetched)
        {
            return m_enum.Next(celt, rgFrameworks, out pceltFetched);
        }

        int IEnumTargetFrameworks.Reset()
        {
            return m_enum.Reset();
        }

        int IEnumTargetFrameworks.Skip(uint celt)
        {
            return m_enum.Skip(celt);
        }

        #endregion
    }

}
