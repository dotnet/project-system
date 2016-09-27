// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Provides a simple wrapper for <see cref="Process.Start(ProcessStartInfo)"/> to allow for testing. Methods are virtual for Moq overridability.
    /// </summary>
    internal class ProcessRunner
    {
        public ProcessRunner()
        {
        }

        public virtual ProcessWrapper Start(ProcessStartInfo info)
        {
            return new ProcessWrapper(Process.Start(info));
        }
    }

    /// <summary>
    /// Provides a simple wrapper for <see cref="Process"/> to allow for testing. Methods are virtual for Moq overridability.
    /// </summary>
    internal class ProcessWrapper
    {
        private readonly Process _process;

        public ProcessWrapper(Process process)
        {
            _process = process;
        }

        public virtual void WaitForExit() => _process.WaitForExit();

        public virtual int ExitCode => _process.ExitCode;

        // Note: for ease of mocking, these are changed from StreamReader to TextReader, as TextReader exposes all the APIs we need
        // and can be mocked by Moq.
        public virtual TextReader StandardOutput => _process.StandardOutput;

        public virtual TextReader StandardError => _process.StandardError;
    }
}
