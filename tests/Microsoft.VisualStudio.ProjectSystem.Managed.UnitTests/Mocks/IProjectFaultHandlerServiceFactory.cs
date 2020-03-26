// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Moq;

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
    }
}
