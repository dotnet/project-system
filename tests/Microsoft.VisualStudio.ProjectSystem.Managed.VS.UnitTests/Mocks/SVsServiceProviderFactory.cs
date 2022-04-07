// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class SVsServiceProviderFactory
    {
        public static SVsServiceProvider Create(object? service = null)
        {
            var mock = new Mock<SVsServiceProvider>();
            mock.Setup<object?>(s => s.GetService(It.IsAny<Type>())).Returns(service);
            return mock.Object;
        }
    }
}
