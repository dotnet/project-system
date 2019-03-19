// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using static Microsoft.VisualStudio.ProjectSystem.VS.Xproj.GlobalJsonRemover;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal static class GlobalJsonSetupFactory
    {
        public static GlobalJsonSetup Create(bool retVal = false)
        {
            var mock = new Mock<GlobalJsonSetup>();
            mock.Setup(g => g.SetupRemoval(It.IsAny<IVsSolution>(), It.IsAny<IServiceProvider>(), It.IsAny<IFileSystem>())).Returns(retVal);
            return mock.Object;
        }
    }
}
