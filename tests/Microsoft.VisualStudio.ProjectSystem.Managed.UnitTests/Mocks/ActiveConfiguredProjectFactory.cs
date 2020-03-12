// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ActiveConfiguredProjectFactory
    {
        public static ActiveConfiguredProject<T> ImplementValue<T>(Func<T> action)
        {
            var mock = new Mock<ActiveConfiguredProject<T>>();

            mock.SetupGet(p => p.Value)
                .Returns(action);

            return mock.Object;
        }
    }
}
