// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Debugger.UI.Interfaces.HotReload
{
    internal static class IHotReloadOptionServiceFactory
    {
        internal static IHotReloadOptionService Create(bool enabledWhenDebugging = true, bool enabledWhenNotDebugging = true)
        {
            var mock = new Mock<IHotReloadOptionService>();

            mock.Setup(options => options.IsHotReloadEnabledAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns<bool, CancellationToken>((debugging, ct) => new ValueTask<bool>(debugging ? enabledWhenDebugging : enabledWhenNotDebugging));

            return mock.Object;
        }
    }
}
