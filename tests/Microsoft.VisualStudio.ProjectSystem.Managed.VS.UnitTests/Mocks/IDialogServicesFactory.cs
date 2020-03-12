// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.UI;
using Moq;

namespace Microsoft.VisualStudio
{
    internal static class IDialogServicesFactory
    {
        public static IDialogServices Create()
        {
            var mock = new Mock<IDialogServices>();
            mock.Setup(s => s.DontShowAgainMessageBox(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => true);
            return mock.Object;
        }
    }
}
