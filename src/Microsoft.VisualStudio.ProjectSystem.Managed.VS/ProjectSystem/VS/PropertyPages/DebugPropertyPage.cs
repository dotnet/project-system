// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [Guid("0273C280-1882-4ED0-9308-52914672E3AA")]
    [ExcludeFromCodeCoverage]
    [ProvideObject(typeof(DebugPropertyPage), RegisterUsing = RegistrationMethod.Assembly)]
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
