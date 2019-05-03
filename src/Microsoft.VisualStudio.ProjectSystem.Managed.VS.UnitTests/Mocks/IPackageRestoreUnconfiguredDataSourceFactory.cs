// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks.Dataflow;
using Moq;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    internal static class IPackageRestoreUnconfiguredDataSourceFactory
    {
        public static IPackageRestoreUnconfiguredDataSource Create()
        {
            var sourceBlock = Mock.Of<IReceivableSourceBlock<IProjectVersionedValue<IVsProjectRestoreInfo2>>>();

            // Moq gets really confused with mocking IProjectValueDataSource<IVsProjectRestoreInfo2>.SourceBlock
            // because of the generic/non-generic version of it. Avoid it.
            return new PackageRestoreUnconfiguredDataSource(sourceBlock);
        }

        private class PackageRestoreUnconfiguredDataSource : IPackageRestoreUnconfiguredDataSource
        {
            public PackageRestoreUnconfiguredDataSource(IReceivableSourceBlock<IProjectVersionedValue<IVsProjectRestoreInfo2>> sourceBlock)
            {
                SourceBlock = sourceBlock;
            }

            public IReceivableSourceBlock<IProjectVersionedValue<IVsProjectRestoreInfo2>> SourceBlock { get; }

            public NamedIdentity DataSourceKey { get; }

            public IComparable DataSourceVersion { get; }

            ISourceBlock<IProjectVersionedValue<object>> IProjectValueDataSource.SourceBlock { get; }

            public IDisposable Join()
            {
                return null;
            }
        }

    }
}
