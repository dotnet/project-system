// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class IPackageRestoreServiceFactory
    {
        public static IPackageRestoreDataSource Create()
        {
            var mock = new Mock<IPackageRestoreDataSource>();

            mock.Setup(s => s.SourceBlock)
                .Returns(DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<RestoreData>>());

            return mock.Object;
        }
    }
}
