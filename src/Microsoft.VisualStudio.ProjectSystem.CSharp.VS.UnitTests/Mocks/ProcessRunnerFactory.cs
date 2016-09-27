// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal static class ProcessRunnerFactory
    {
        public static ProcessRunner CreateRunner()
        {
            return Mock.Of<ProcessRunner>();
        }

        public static ProcessRunner ImplementRunner(string outputText = "", string errorText = "", int exitCode = 0)
        {
            var mock = new Mock<ProcessRunner>();

            mock.Setup(pr => pr.Start(It.IsAny<ProcessStartInfo>())).Returns(CreateProcess(outputText, errorText, exitCode));

            return mock.Object;
        }

        public static Process CreateProcess(string outputText = "", string errorText = "", int exitCode = 0)
        {
            var processMock = new Mock<Process>();
            var soMock = new Mock<StreamReader>();
            var seMock = new Mock<StreamReader>();

            processMock.Setup(p => p.ExitCode).Returns(exitCode);

            // Mock standard output and standard error
            soMock.Setup(so => so.ReadToEnd()).Returns(outputText);
            seMock.Setup(se => se.ReadToEnd()).Returns(errorText);
            processMock.Setup(p => p.StandardOutput).Returns(soMock.Object);
            processMock.Setup(p => p.StandardError).Returns(seMock.Object);

            return null;
        }
    }
}
