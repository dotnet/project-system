using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    public class PropertyFake
    {
        private string _name;
        private object _value;
        private Type _type;

        public PropertyFake(string name, object value)
        {
            Debug.Assert(name != null && name != "");
            Debug.Assert(value != null, "You need to pass in the Type if the value is null");
            _name = name;
            _value = value;
            _type = value.GetType();
        }

        public PropertyFake(string name, Type type, object value)
        {
            Debug.Assert(name != null && name != "" && type != null);
            _name = name;
            _value = value;
            _type = type;
            System.Diagnostics.Debug.Assert(value == null || type.IsAssignableFrom(value.GetType()));
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public Object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public Type Type
        {
            get
            {
                return _type;
            }
        }
    }
}
