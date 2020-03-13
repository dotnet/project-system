// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class ILaunchSettingsUIProviderFactory
    {
        public static ILaunchSettingsUIProvider Create(string commandName, string friendlyName)
        {
            var mock = new Mock<ILaunchSettingsUIProvider>();
            mock.Setup(t => t.CommandName).Returns(commandName);
            mock.Setup(t => t.FriendlyName).Returns(friendlyName);
            return mock.Object;
        }
    }
}
