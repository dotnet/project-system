// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.Editors.UnitTests
{
    internal partial class Microsoft_VisualStudio_Editors_PropertyPages_WPF_ApplicationPropPageVBWPFAccessor : BaseAccessor
    {
        internal global::Microsoft.VisualStudio.Editors.PropertyPages.PropertyControlData GetPropertyControlData(int PropertyId)
        {
            object[] args = new object[] {
                PropertyId};
            global::Microsoft.VisualStudio.Editors.PropertyPages.PropertyControlData ret = ((global::Microsoft.VisualStudio.Editors.PropertyPages.PropertyControlData)(m_privateObject.Invoke("GetPropertyControlData", new System.Type[] {
                    typeof(int)}, args)));
            return ret;
        }
    }

}
