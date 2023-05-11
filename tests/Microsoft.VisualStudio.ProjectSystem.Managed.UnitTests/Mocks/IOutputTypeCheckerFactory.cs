// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem;

internal static class IOutputTypeCheckerFactory
{
    public static IOutputTypeChecker Create(bool? isLibrary = false, bool? isConsole = false)
    {
        var mock = new Mock<IOutputTypeChecker>();

        if (isLibrary is not null)
        {
            mock.Setup(m => m.IsLibraryAsync()).ReturnsAsync(isLibrary.Value);
        }

        if (isConsole is not null)
        {
            mock.Setup(m => m.IsConsoleAsync()).ReturnsAsync(isConsole.Value);
        }

        return mock.Object;
    }
}
