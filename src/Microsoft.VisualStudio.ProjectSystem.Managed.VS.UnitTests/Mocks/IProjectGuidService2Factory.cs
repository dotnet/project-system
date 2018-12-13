// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

#pragma warning disable RS0030 // Symbol IProjectGuidService2 is banned

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectGuidService2Factory
    {
        public static IProjectGuidService ImplementGetProjectGuidAsync(Guid result)
        {
            return ImplementGetProjectGuidAsync(() => result);
        }

        public static IProjectGuidService ImplementGetProjectGuidAsync(Func<Guid> action)
        {
            var mock = new Mock<IProjectGuidService2>();
            mock.Setup(s => s.GetProjectGuidAsync())
                .ReturnsAsync(action);

            // All IProjectGuidService2 have to be IProjectGuidService instances
            return mock.As<IProjectGuidService>().Object;
        }
    }
}
