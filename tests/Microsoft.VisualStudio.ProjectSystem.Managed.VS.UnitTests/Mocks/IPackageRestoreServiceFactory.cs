// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.PackageRestore;

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
