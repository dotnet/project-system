// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
