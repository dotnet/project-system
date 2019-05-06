// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class IVsProjectRestoreInfo2Factory
    {
        public static IVsProjectRestoreInfo2 Create()
        {
            return Mock.Of<IVsProjectRestoreInfo2>();
        }
    }
}
