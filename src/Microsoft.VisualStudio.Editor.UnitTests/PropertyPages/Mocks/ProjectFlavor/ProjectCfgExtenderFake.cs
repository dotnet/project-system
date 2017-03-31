// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks.ProjectFlavor
{
    [ClassInterface(ClassInterfaceType.None)]
    class ProjectCfgExtenderFake
        :
        IProjectDesignerTestFlavor_ProjectCfgExtenderProperties,
        IFilterProperties,
        IPropertyFilterOptionsContainer
    {
        PropertyFilterOptions Fake_propertyFilterOptions = new PropertyFilterOptions();

        #region IFilterProperties Members

        public vsFilterProperties IsPropertyHidden(string propertyName)
        {
            return Fake_propertyFilterOptions.IsPropertyHidden(propertyName);
        }

        #endregion

        #region IProjectDesignerTestFlavor_ProjectExtenderProperties Members

        public string ExtendedProperty2
        {
            get
            {
                return "ExtendedProperty2";
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public string ExtendedReadOnlyProperty2
        {
            get
            {
                return "ExtendedReadOnlyProperty2";
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
    interface IProjectDesignerTestFlavor_ProjectCfgExtenderProperties
    {
        string ExtendedProperty2
        {
            get;
            set;
        }
        string ExtendedReadOnlyProperty2
        {
            get;
        }
    }

}
