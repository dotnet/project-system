// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio
{
    [Guid("D15F5C78-D04F-45FD-AEA2-D7982D8FA429")]
    [PackageRegistration(AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase, UseManagedResourcesOnly = true)]
    internal class VisualBasicProjectSystemPackage : AsyncPackage
    {
        public VisualBasicProjectSystemPackage()
        {
        }
    }
}
