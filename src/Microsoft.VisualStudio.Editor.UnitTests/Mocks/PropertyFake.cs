// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class PropertyFake : Property
    {
        #region Property Members

        private string _name;
        private object _value;

        public PropertyFake(string name, object value)
        {
            _name = name;
            _value = value;
        }

        object Property.Application
        {
            get { throw new Exception("PropertyFake: The method or operation is not implemented."); }
        }

        Properties Property.Collection
        {
            get { throw new Exception("PropertyFake: The method or operation is not implemented."); }
        }

        DTE Property.DTE
        {
            get { throw new Exception("PropertyFake: The method or operation is not implemented."); }
        }

        string Property.Name
        {
            get {
                return _name;
            }
        }

        short Property.NumIndices
        {
            get { throw new Exception("PropertyFake: The method or operation is not implemented."); }
        }

        object Property.Object
        {
            get
            {
                throw new Exception("PropertyFake: The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("PropertyFake: The method or operation is not implemented.");
            }
        }

        Properties Property.Parent
        {
            get { throw new Exception("PropertyFake: The method or operation is not implemented."); }
        }

        object Property.Value
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

        object Property.get_IndexedValue(object Index1, object Index2, object Index3, object Index4)
        {
            throw new Exception("PropertyFake: The method or operation is not implemented.");
        }

        void Property.let_Value(object lppvReturn)
        {
            throw new Exception("PropertyFake: The method or operation is not implemented.");
        }

        void Property.set_IndexedValue(object Index1, object Index2, object Index3, object Index4, object Val)
        {
            throw new Exception("PropertyFake: The method or operation is not implemented.");
        }

        #endregion
    }
}
