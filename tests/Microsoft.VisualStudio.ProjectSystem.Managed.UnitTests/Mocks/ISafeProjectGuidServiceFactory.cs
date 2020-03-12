// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ISafeProjectGuidServiceFactory
    {
        public static ISafeProjectGuidService ImplementGetProjectGuidAsync(Guid guid)
        {
            var mock = new Mock<ISafeProjectGuidService>();
            mock.Setup(s => s.GetProjectGuidAsync())
                .ReturnsAsync(guid);

            return mock.Object;
        }
    }
}
