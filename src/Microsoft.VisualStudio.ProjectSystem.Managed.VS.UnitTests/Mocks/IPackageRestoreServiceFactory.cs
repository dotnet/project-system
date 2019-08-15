// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class IPackageRestoreServiceFactory
    {
        public static IPackageRestoreService Create()
        {
            var mock = new Mock<IPackageRestoreService>();

            mock.Setup(s => s.RestoreData)
                .Returns(DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<RestoreData>>());

            return mock.Object;
        }
    }
}
