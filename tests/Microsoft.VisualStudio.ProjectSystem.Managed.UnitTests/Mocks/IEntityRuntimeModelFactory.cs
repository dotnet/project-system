// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IEntityRuntimeModelFactory
    {
        public static IEntityRuntimeModel Create()
        {
            var mock = new Mock<IEntityRuntimeModel>();
            return mock.Object;
        }
    }
}
