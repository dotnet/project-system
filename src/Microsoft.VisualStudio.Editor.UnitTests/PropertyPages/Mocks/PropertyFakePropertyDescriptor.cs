// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    interface IPropertyContainer
    {
        object GetValue(string propertyName, Type returnType);
        void SetValue(string propertyName, Type returnType, object value);
    }

    class PropertyFakePropertyDescriptor : PropertyDescriptor
    {
        PropertyFake _property;

        public PropertyFakePropertyDescriptor(PropertyFake property)
            : base(property.Name, new Attribute[] { })
        {
            Debug.Assert(property != null);
            _property = property;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get { return typeof(Property); }
        }

        public override object GetValue(object component)
        {
            if (component is ICustomTypeDescriptor)
            {
                component = ((ICustomTypeDescriptor)component).GetPropertyOwner(this);
            }
            Debug.Assert(component is IPropertyContainer, "Something's wrong in the test case or fake property hosting.");
            
            return ((IPropertyContainer)component).GetValue(_property.Name, _property.Type);
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get
            {
                return _property.Type;
            }
        }

        public override void ResetValue(object component)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void SetValue(object component, object value)
        {
            ((IPropertyContainer)component).SetValue(_property.Name, _property.Type, value);
            OnValueChanged(component, EventArgs.Empty);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

    }
}
