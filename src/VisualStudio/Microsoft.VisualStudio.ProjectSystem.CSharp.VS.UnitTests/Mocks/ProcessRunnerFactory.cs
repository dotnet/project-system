// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Moq;
using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    internal static class ProcessRunnerFactory
    {
        public static ProcessRunner CreateRunner()
        {
            return Mock.Of<ProcessRunner>();
        }

        public static ProcessRunner ImplementRunner(Action<ProcessStartInfo> callback, string outputText = "", string errorText = "", int exitCode = 0)
        {
            var mock = new Mock<ProcessRunner>();

            mock.Setup(pr => pr.Start(It.IsAny<ProcessStartInfo>())).Returns(CreateProcess(outputText, errorText, exitCode)).Callback(callback);

            return mock.Object;
        }

        public static ProcessWrapper CreateProcess(string outputText = "", string errorText = "", int exitCode = 0)
        {
            var processMock = new Mock<ProcessWrapper>(new object[] { null });

            processMock.Setup(p => p.ExitCode).Returns(exitCode);

            // Mock standard output and standard error
            processMock.Setup(p => p.AddOutputDataReceivedHandler(It.IsAny<Action<string>>())).Callback<Action<string>>(h =>
            {
                if (!string.IsNullOrWhiteSpace(outputText)) h(outputText);
            });
            processMock.Setup(p => p.AddErrorDataReceivedHandler(It.IsAny<Action<string>>())).Callback<Action<string>>(h =>
            {
                if (!string.IsNullOrWhiteSpace(errorText)) h(errorText);
            });

            return processMock.Object;
        }
    }
}
