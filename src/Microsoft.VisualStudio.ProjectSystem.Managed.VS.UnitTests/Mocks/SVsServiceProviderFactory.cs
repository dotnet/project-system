// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class SVsServiceProviderFactory
    {
        public static SVsServiceProvider Create(IVsAddProjectItemDlg dlg = null)
        {
            var mock = new Mock<SVsServiceProvider>();
            mock.Setup(s => s.GetService(It.IsAny<Type>())).Returns(dlg);
            return mock.Object;
        }
    }
}