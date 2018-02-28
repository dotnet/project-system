// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectGuidService2Factory
    {
        public static IProjectGuidService2 ImplementGetProjectGuidAsync(Guid result)
        {
            var mock = new Mock<IProjectGuidService2>();
            mock.Setup(s => s.GetProjectGuidAsync())
                .ReturnsAsync(result);


            return mock.Object;
        }
    }
}
