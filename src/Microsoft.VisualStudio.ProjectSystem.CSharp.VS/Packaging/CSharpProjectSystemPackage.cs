// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Packaging
{
    [Guid(PackageGuid)]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    internal class CSharpProjectSystemPackage : AsyncPackage
    {
        public const string PackageGuid = "860A27C0-B665-47F3-BC12-637E16A1050A";

        public CSharpProjectSystemPackage()
        {
        }


    }
}
