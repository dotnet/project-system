// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

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

        public virtual void AddOutputDataReceivedHandler(Action<string> handler)
        {
            _process.OutputDataReceived += new DataReceivedEventHandler((sender, o) =>
            {
                handler(o.Data);
            });
        }

        public virtual void AddErrorDataReceivedHandler(Action<string> handler)
        {
            _process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                handler(e.Data);
            });
        }

        public virtual void BeginOutputReadLine()
        {
            _process.BeginOutputReadLine();
        }

        public virtual void BeginErrorReadLine()
        {
            _process.BeginErrorReadLine();
        }
    }
}
