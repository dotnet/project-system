// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.WPF;

namespace Microsoft.VisualStudio.ProjectSystem;

internal class IApplicationXamlFileAccessorFactory
{
    public static IApplicationXamlFileAccessor Create(
        string? startupUri = null,
        string? shutdownMode = null,
        Func<Task<string?>>? getStartupUri = null,
        Func<Task<string?>>? getShutdownMode = null,
        Func<string, Task>? setStartupUri = null,
        Func<string, Task>? setShutdownMode = null)
    {
        var mock = new Mock<IApplicationXamlFileAccessor>();

        if (getStartupUri is not null)
        {
            mock.Setup(m => m.GetStartupUriAsync())
                .Returns(getStartupUri);
        }
        else
        {
            mock.Setup(m => m.GetStartupUriAsync())
                .ReturnsAsync(startupUri);
        }

        if (getShutdownMode is not null)
        {
            mock.Setup(m => m.GetShutdownModeAsync())
                .Returns(getShutdownMode);
        }
        else
        {
            mock.Setup(m => m.GetShutdownModeAsync())
                .ReturnsAsync(shutdownMode);
        }

        if (setStartupUri is not null)
        {
            mock.Setup(m => m.SetStartupUriAsync(It.IsAny<string>()))
                .Returns(setStartupUri);
        }

        if (setShutdownMode is not null)
        {
            mock.Setup(m => m.SetShutdownModeAsync(It.IsAny<string>()))
                .Returns(setShutdownMode);
        }

        return mock.Object;
    }
}
