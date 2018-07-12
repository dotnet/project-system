// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{

    [Export(typeof(IPageMetadata))]
    internal partial class DebugPropertyPageMetaData : IPageMetadata
    {
        bool IPageMetadata.HasConfigurationCondition { get { return false; } }
        string IPageMetadata.Name { get { return DebugPropertyPage.PageName; } }
        Guid IPageMetadata.PageGuid { get { return typeof(DebugPropertyPage).GUID; } }
        int IPageMetadata.PageOrder { get { return 30; } }
    }

    [Guid("0273C280-1882-4ED0-9308-52914672E3AA")]
    [ExcludeFromCodeCoverage]
    internal partial class DebugPropertyPage : WpfBasedPropertyPage
    {

        internal static readonly string PageName = PropertyPageResources.DebugPropertyPageTitle;

        public DebugPropertyPage()
        {
        }

        protected override PropertyPageViewModel CreatePropertyPageViewModel()
        {
            return new DebugPageViewModel();
        }

        protected override PropertyPageControl CreatePropertyPageControl()
        {
            return new DebugPageControl();
        }

        protected override string PropertyPageName
        {
            get
            {
                return PageName;
            }
        }


    }
}

