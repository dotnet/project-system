// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    internal sealed partial class UnconfiguredProjectHostProvider
    {
        /// <summary>
        /// Wrapper host object for cross targeting projects that wraps the underlying host object.
        /// TODO: This host object needs to implement other IVSXXX interfaces suchas as IVsContainedLanguageProjectNameProvider.
        /// </summary>
        private sealed class HostObject : IVsHierarchy
        {
            private readonly IVsHierarchy _innerHierarchy;

            public HostObject(IVsHierarchy innerHierarchy)
            {
                Requires.NotNull(innerHierarchy, nameof(innerHierarchy));

                _innerHierarchy = innerHierarchy;
            }

            public Int32 AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out UInt32 pdwCookie)
            {
                return _innerHierarchy.AdviseHierarchyEvents(pEventSink, out pdwCookie);
            }

            public Int32 Close()
            {
                return _innerHierarchy.Close();
            }

            public Int32 GetCanonicalName(UInt32 itemid, out String pbstrName)
            {
                return _innerHierarchy.GetCanonicalName(itemid, out pbstrName);
            }

            public Int32 GetGuidProperty(UInt32 itemid, Int32 propid, out Guid pguid)
            {
                return _innerHierarchy.GetGuidProperty(itemid, propid, out pguid);
            }

            public Int32 GetNestedHierarchy(UInt32 itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out UInt32 pitemidNested)
            {
                return _innerHierarchy.GetNestedHierarchy(itemid, ref iidHierarchyNested, out ppHierarchyNested, out pitemidNested);
            }

            public Int32 GetProperty(UInt32 itemid, Int32 propid, out Object pvar)
            {
                switch (propid)
                {
                    case (int)__VSHPROPID7.VSHPROPID_IsSharedItem:
                        // In our world everything is shared
                        pvar = true;
                        return VSConstants.S_OK;

                    case (int)__VSHPROPID7.VSHPROPID_SharedProjectHierarchy:
                        pvar = _innerHierarchy;
                        return VSConstants.S_OK;

                    default:
                        return _innerHierarchy.GetProperty(itemid, propid, out pvar);
                }
            }

            public Int32 GetSite(out IOLEServiceProvider ppSP)
            {
                return _innerHierarchy.GetSite(out ppSP);
            }

            public Int32 ParseCanonicalName(String pszName, out UInt32 pitemid)
            {
                return _innerHierarchy.ParseCanonicalName(pszName, out pitemid);
            }

            public Int32 QueryClose(out Int32 pfCanClose)
            {
                return _innerHierarchy.QueryClose(out pfCanClose);
            }

            public Int32 SetGuidProperty(UInt32 itemid, Int32 propid, ref Guid rguid)
            {
                return _innerHierarchy.SetGuidProperty(itemid, propid, ref rguid);
            }

            public Int32 SetProperty(UInt32 itemid, Int32 propid, Object var)
            {
                return _innerHierarchy.SetProperty(itemid, propid, var);
            }

            public Int32 SetSite(IOLEServiceProvider psp)
            {
                return _innerHierarchy.SetSite(psp);
            }

            public Int32 UnadviseHierarchyEvents(UInt32 dwCookie)
            {
                return _innerHierarchy.UnadviseHierarchyEvents(dwCookie);
            }

            public Int32 Unused0()
            {
                return _innerHierarchy.Unused0();
            }

            public Int32 Unused1()
            {
                return _innerHierarchy.Unused1();
            }

            public Int32 Unused2()
            {
                return _innerHierarchy.Unused2();
            }

            public Int32 Unused3()
            {
                return _innerHierarchy.Unused3();
            }

            public Int32 Unused4()
            {
                return _innerHierarchy.Unused4();
            }
        }
    }
}
