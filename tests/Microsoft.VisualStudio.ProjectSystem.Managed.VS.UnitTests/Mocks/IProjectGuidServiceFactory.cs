﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Moq;

#pragma warning disable RS0030 // This is the one place where IProjectGuidService is allowed to be referenced

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IProjectGuidServiceFactory
    {
        public static IProjectGuidService ImplementProjectGuid(Guid result)
        {
            var mock = new Mock<IProjectGuidService>();
            mock.Setup(s => s.ProjectGuid)
                .Returns(result);

            return mock.Object;
        }
    }
}
