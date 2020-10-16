// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Query;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IPropertyPageQueryCacheProviderFactory
    {
        internal static IPropertyPageQueryCacheProvider Create()
        {
            var cache = IPropertyPageQueryCacheFactory.Create();
            return Create(cache);
        }

        internal static IPropertyPageQueryCacheProvider Create(IPropertyPageQueryCache cache)
        {
            var mock = new Mock<IPropertyPageQueryCacheProvider>();
            mock.Setup(f => f.CreateCache(It.IsAny<UnconfiguredProject>())).Returns(cache);

            return mock.Object;
        }
    }
}
