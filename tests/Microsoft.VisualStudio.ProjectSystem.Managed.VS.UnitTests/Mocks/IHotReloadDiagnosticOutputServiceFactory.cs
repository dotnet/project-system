// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IHotReloadDiagnosticOutputServiceFactory
    {
        public static IHotReloadDiagnosticOutputService Create(Action? writeLineCallback = null)
        {
            var mock = new Mock<IHotReloadDiagnosticOutputService>();

            if (writeLineCallback is not null)
            {
                mock.Setup(service => service.WriteLine(It.IsAny<HotReloadLogMessage>(), CancellationToken.None))
                    .Callback(writeLineCallback);
            }

            return mock.Object;
        }
    }
}
