// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
