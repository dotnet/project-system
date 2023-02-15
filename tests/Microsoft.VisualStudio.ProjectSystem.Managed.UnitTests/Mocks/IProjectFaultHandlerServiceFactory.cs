// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectFaultHandlerServiceFactory
    {
        public static IProjectFaultHandlerService Create()
        {
            return Mock.Of<IProjectFaultHandlerService>();
        }

        public static IProjectFaultHandlerService ImplementHandleFaultAsync(Action<Exception, ErrorReportSettings?, ProjectFaultSeverity, UnconfiguredProject?> action)
        {
            var mock = new Mock<IProjectFaultHandlerService>();
            mock.Setup(s => s.HandleFaultAsync(It.IsAny<Exception>(), It.IsAny<ErrorReportSettings?>(), It.IsAny<ProjectFaultSeverity>(), It.IsAny<UnconfiguredProject?>()))
                .ReturnsAsync(action);

            return mock.Object;
        }

        public static IProjectFaultHandlerService ImplementForget(Action<Task, ErrorReportSettings, ProjectFaultSeverity, UnconfiguredProject?> action)
        {
            var mock = new Mock<IProjectFaultHandlerService>();
            mock.Setup(s => s.RegisterFaultHandler(It.IsAny<Task>(), It.IsAny<ErrorReportSettings>(), It.IsAny<ProjectFaultSeverity>(), It.IsAny<UnconfiguredProject?>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
