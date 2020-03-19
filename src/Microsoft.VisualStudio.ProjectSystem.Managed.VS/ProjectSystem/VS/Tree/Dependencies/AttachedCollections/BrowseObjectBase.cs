// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections
{
    /// <summary>
    /// This is a slightly modified copy of Microsoft.VisualStudio.Shell.LocalizableProperties.
    /// http://index/#Microsoft.VisualStudio.Shell.12.0/LocalizableProperties.cs.html
    /// Unfortunately we can't reuse that class because the GetComponentName method on
    /// it is not virtual, so we can't provide a name string for the VS Property Grid's
    /// combo box (which shows ComponentName in bold and ClassName in regular to the
    /// right from it)
    /// </summary>
    [ComVisible(true)]
    internal abstract class BrowseObjectBase : ICustomTypeDescriptor
    {
        [Browsable(false)]
        public string ExtenderCATID => "";

        public abstract string GetComponentName();

        public abstract string GetClassName();

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);

        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);

        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        public PropertyDescriptorCollection GetProperties() => GetProperties(null);

        public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(this, attributes, true);

            var newList = new PropertyDescriptor[props.Count];

            for (int i = 0; i < props.Count; i++)
            {
                newList[i] = new DesignPropertyDescriptor(props[i]);
            }

            return new PropertyDescriptorCollection(newList);
        }

        public virtual TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);
    }
}
