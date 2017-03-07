using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class ProjectItemWithCustomToolFake : ProjectItemFake
    {
        public ProjectItemWithCustomToolFake(object initialCustomToolValue)
            : this(initialCustomToolValue, string.Empty)
        {
        }

        public ProjectItemWithCustomToolFake(object initialCustomToolValue, string initialCustomToolNamespace)
        {
            Fake_PropertiesCollection.Fake_PropertiesDictionary.Add("CustomTool", new PropertyFake("CustomTool", initialCustomToolValue));
            Fake_PropertiesCollection.Fake_PropertiesDictionary.Add("CustomToolNamespace", new PropertyFake("CustomToolNamespace", initialCustomToolNamespace));
        }

    }

}
