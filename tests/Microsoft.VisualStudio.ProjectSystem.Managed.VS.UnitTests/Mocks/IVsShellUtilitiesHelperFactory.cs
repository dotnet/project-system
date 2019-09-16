// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UI
{
    internal static class IVsShellUtilitiesHelperFactory
    {
        public static IVsShellUtilitiesHelper Create(string? localAppDataFolder = null, Version? vsVersion = null)
        {
            var mock = new Mock<IVsShellUtilitiesHelper>();
            if (localAppDataFolder != null)
            {
                mock.Setup(s => s.GetLocalAppDataFolderAsync(It.IsAny<IVsService<IVsShell>>()))
                    .ReturnsAsync(() => localAppDataFolder);
            }

            if (vsVersion != null)
            {
                mock.Setup(s => s.GetVSVersionAsync(It.IsAny<IVsService<IVsAppId>>()))
                    .ReturnsAsync(() => vsVersion);
            }

            return mock.Object;
        }
    }
}
