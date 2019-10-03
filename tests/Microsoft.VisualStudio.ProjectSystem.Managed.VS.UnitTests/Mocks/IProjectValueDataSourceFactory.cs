// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks.Dataflow;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IProjectValueDataSourceFactory
    {
        public static IProjectValueDataSource<T> CreateInstance<T>()
        {
            var mock = new Mock<IProjectValueDataSource<T>>();
            mock.SetupGet(m => m.SourceBlock).Returns(Mock.Of<IReceivableSourceBlock<IProjectVersionedValue<T>>>());
            return mock.Object;
        }
    }
}
