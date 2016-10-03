// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    internal class ManagedProjectSystemPackage : AsyncPackage
    {
        public const string PackageGuid = "A4F9D880-9492-4072-8BF3-2B5EEEDC9E68";
        public const string DotnetProjectSystemCommandSet = "{568ABDF7-D522-474D-9EED-34B5E5095BA5}";
        public const long EditCsprojCmdid = 0x1001;

        public ManagedProjectSystemPackage()
        {
        }
    }
}
