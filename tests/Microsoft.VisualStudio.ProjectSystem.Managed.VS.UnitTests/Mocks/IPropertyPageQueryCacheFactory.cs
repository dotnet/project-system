// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Query;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class IPropertyPageQueryCacheFactory
    {
        internal static IPropertyPageQueryCache Create()
        {
            var mock = new Mock<IPropertyPageQueryCache>();

            return mock.Object;
        }
    }
}
