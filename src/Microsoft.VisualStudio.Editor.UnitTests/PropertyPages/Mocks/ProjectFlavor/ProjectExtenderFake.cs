// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks.ProjectFlavor
{
    [ClassInterface(ClassInterfaceType.None)]
    class ProjectExtenderFake
        :
        IProjectDesignerTestFlavor_ProjectExtenderProperties,
        EnvDTE.IFilterProperties,
        IPropertyFilterOptionsContainer
    {
        PropertyFilterOptions Fake_propertyFilterOptions = new PropertyFilterOptions();

        #region IFilterProperties Members

        public EnvDTE.vsFilterProperties IsPropertyHidden(string propertyName)
        {
            return Fake_propertyFilterOptions.IsPropertyHidden(propertyName);
        }

        #endregion

        #region IProjectDesignerTestFlavor_ProjectExtenderProperties Members

        public string ExtendedProperty1
        {
            get
            {
                return "ExtendedProperty1";
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public string ExtendedReadOnlyProperty1
        {
            get
            {
                return "ExtendedReadOnlyProperty1";
            }
        }

        #endregion

        #region PropertyFilterOptionsContainer Members

        PropertyFilterOptions IPropertyFilterOptionsContainer.PropertyFilterOptions
        {
            get
            {
                return Fake_propertyFilterOptions;
            }
        }

        #endregion
    }

    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    interface IProjectDesignerTestFlavor_ProjectExtenderProperties
    {
        string ExtendedProperty1
        {
            get;
            set;
        }
        string ExtendedReadOnlyProperty1
        {
            get;
        }
    }

}

