using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class PropertiesFake : Properties
    {
        #region Properties Members

        public Dictionary<string, Property> Fake_PropertiesDictionary = new Dictionary<string, Property>();

        public object Application
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public int Count
        {
            get
            {
                return Fake_PropertiesDictionary.Count;
            }
        }

        public DTE DTE
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return Fake_PropertiesDictionary.Values.GetEnumerator();
        }

        public Property Item(object index)
        {
            if (index is string)
            {
                return Fake_PropertiesDictionary[(string)index];
            }

            throw new Exception("The method or operation is not implemented.");
        }

        public object Parent
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion
    }
}
