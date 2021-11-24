// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IDebuggerSettingsFactory
    {
        internal static IDebuggerSettings Create(bool encEnabled = true, bool nonDebugHotReloadEnabled = true)
        {
            var mock = new Mock<IDebuggerSettings>();

            mock.Setup(settings => settings.IsEncEnabledAsync()).ReturnsAsync(encEnabled);
            mock.Setup(settings => settings.IsNonDebugHotReloadEnabledAsync()).ReturnsAsync(nonDebugHotReloadEnabled);

            return mock.Object;
        }
    }
}
