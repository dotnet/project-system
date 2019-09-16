// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [Guid("0273C280-1882-4ED0-9308-52914672E3AA")]
    [ExcludeFromCodeCoverage]
    [ProvideObject(typeof(DebugPropertyPage), RegisterUsing = RegistrationMethod.CodeBase)]
    internal class DebugPropertyPage : WpfBasedPropertyPage
    {
        internal static readonly string PageName = PropertyPageResources.DebugPropertyPageTitle;

        protected override PropertyPageViewModel CreatePropertyPageViewModel()
        {
            return new DebugPageViewModel();
        }

        protected override PropertyPageControl CreatePropertyPageControl()
        {
            return new DebugPageControl();
        }

        protected override string PropertyPageName => PageName;
    }
}
