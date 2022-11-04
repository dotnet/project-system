// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.PackageRestore;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class IPackageRestoreUnconfiguredInputDataSourceFactory
    {
        public static IPackageRestoreUnconfiguredInputDataSource Create()
        {
            var sourceBlock = Mock.Of<IReceivableSourceBlock<IProjectVersionedValue<PackageRestoreUnconfiguredInput>>>();

            // Moq gets really confused with mocking IProjectValueDataSource<IVsProjectRestoreInfo2>.SourceBlock
            // because of the generic/non-generic version of it. Avoid it.
            return new PackageRestoreUnconfiguredDataSource(sourceBlock);
        }

        private class PackageRestoreUnconfiguredDataSource : IPackageRestoreUnconfiguredInputDataSource
        {
            public PackageRestoreUnconfiguredDataSource(IReceivableSourceBlock<IProjectVersionedValue<PackageRestoreUnconfiguredInput>> sourceBlock)
            {
                SourceBlock = sourceBlock;
                DataSourceKey = new NamedIdentity();
                DataSourceVersion = 1;
            }

            public IReceivableSourceBlock<IProjectVersionedValue<PackageRestoreUnconfiguredInput>> SourceBlock { get; }

            public NamedIdentity DataSourceKey { get; }

            public IComparable DataSourceVersion { get; }

            ISourceBlock<IProjectVersionedValue<object>> IProjectValueDataSource.SourceBlock => SourceBlock;

            public IDisposable? Join()
            {
                return null;
            }
        }
    }
}
