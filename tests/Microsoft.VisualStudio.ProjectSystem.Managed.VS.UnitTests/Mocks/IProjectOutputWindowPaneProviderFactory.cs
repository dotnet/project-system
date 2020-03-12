// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Logging
{
    internal static class IProjectOutputWindowPaneProviderFactory
    {
        public static IProjectOutputWindowPaneProvider Create()
        {
            return Mock.Of<IProjectOutputWindowPaneProvider>();
        }

        public static IProjectOutputWindowPaneProvider ImplementGetOutputWindowPaneAsync(IVsOutputWindowPane pane)
        {
            var mock = new Mock<IProjectOutputWindowPaneProvider>();
            mock.Setup(p => p.GetOutputWindowPaneAsync())
                .ReturnsAsync(pane);

            return mock.Object;
        }
    }
}
