// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    /// <remarks>
    /// <para>
    /// This is a slightly modified copy of Microsoft.VisualStudio.Shell.LocalizableProperties.
    /// http://index/#Microsoft.VisualStudio.Shell.12.0/LocalizableProperties.cs.html
    /// Unfortunately we can't reuse that class because the GetComponentName method on
    /// it is not virtual, so we can't provide a name string for the VS Property Grid's
    /// combo box (which shows ComponentName in bold and ClassName in regular to the
    /// right from it)
    /// </para>
    /// <param>
    /// PR https://dev.azure.com/devdiv/DevDiv/_git/VS/pullrequest/248337 makes the method
    /// virtual, so once that update becomes available this type can be removed and
    /// <see cref="LocalizableProperties"/> used directly in its place.
    /// </param>
    /// </remarks>
    internal abstract class BrowseObjectBase : ICustomTypeDescriptor
    {
#pragma warning disable CA1822
        [Browsable(false)]
        public string ExtenderCATID => "";
#pragma warning restore CA1822

        public abstract string GetComponentName();

        public abstract string GetClassName();

        AttributeCollection ICustomTypeDescriptor.GetAttributes() => TypeDescriptor.GetAttributes(this, true);

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => TypeDescriptor.GetEvents(this, true);

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => this;

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => ((ICustomTypeDescriptor)this).GetProperties(null);

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(this, attributes, true);

            var newList = new PropertyDescriptor[props.Count];

            for (int i = 0; i < props.Count; i++)
            {
                newList[i] = new DesignPropertyDescriptor(props[i]);
            }

            return new PropertyDescriptorCollection(newList);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter() => TypeDescriptor.GetConverter(this, true);
    }
}
