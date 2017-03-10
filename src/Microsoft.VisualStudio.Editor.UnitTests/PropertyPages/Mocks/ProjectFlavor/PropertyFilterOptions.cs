// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks.ProjectFlavor
{
    interface IPropertyFilterOptionsContainer
    {
        PropertyFilterOptions PropertyFilterOptions { get; }
    }

    class PropertyFilterOptions
    {
        public bool AllPropertiesAreHidden = false;
        public bool AllPropertiesAreReadOnly = false;
        public List<string> HiddenPropertyNames = new List<string>();
        public List<string> ReadOnlyPropertyNames = new List<string>();

        public vsFilterProperties IsPropertyHidden(string propertyName)
        {
            // By default, property is visible to property pages
            vsFilterProperties propertyHidden = vsFilterProperties.vsFilterPropertiesNone;

            if (AllPropertiesAreHidden || HiddenPropertyNames.Contains(propertyName))
            {
                // Don't hide FullPath for "all" unless explicit - too integral to the property page functionality
                if (propertyName.Equals("FullPath") && !HiddenPropertyNames.Contains("FullPath"))
                {
                }
                else
                {
                    // Property is hidden
                    propertyHidden = vsFilterProperties.vsFilterPropertiesAll;
                }
            }
            else if (AllPropertiesAreReadOnly || ReadOnlyPropertyNames.Contains(propertyName))
            {
                // Property is read-only
                propertyHidden = vsFilterProperties.vsFilterPropertiesSet;
            }
            return propertyHidden;
        }

    }
}
