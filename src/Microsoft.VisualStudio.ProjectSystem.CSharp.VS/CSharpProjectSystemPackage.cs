// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio
{
    [Guid("860A27C0-B665-47F3-BC12-637E16A1050A")]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    internal class CSharpProjectSystemPackage : AsyncPackage
    {
        public CSharpProjectSystemPackage()
        {
        }
    }
}
