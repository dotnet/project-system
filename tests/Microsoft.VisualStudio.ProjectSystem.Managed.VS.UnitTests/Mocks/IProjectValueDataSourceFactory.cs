// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;

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
