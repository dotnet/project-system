// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.Mocks
{
    public static class IRemoteAuthenticationProviderFactory
    {
        internal static IRemoteAuthenticationProvider Create(string name, string displayName)
        {
            var provider = new Mock<IRemoteAuthenticationProvider>();

            provider.SetupGet(o => o.Name).Returns(name);
            provider.SetupGet(o => o.DisplayName).Returns(displayName);

            return provider.Object;
        }
    }
}
