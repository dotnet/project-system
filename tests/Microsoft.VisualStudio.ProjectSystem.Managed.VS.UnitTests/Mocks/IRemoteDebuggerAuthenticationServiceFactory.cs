// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.Mocks
{
    public static class IRemoteDebuggerAuthenticationServiceFactory
    {
        internal static IRemoteDebuggerAuthenticationService Create(params IRemoteAuthenticationProvider[] providers)
        {
            var service = new Mock<IRemoteDebuggerAuthenticationService>();

            service.Setup(s => s.GetRemoteAuthenticationModes()).Returns(providers);

            return service.Object;
        }
    }
}
