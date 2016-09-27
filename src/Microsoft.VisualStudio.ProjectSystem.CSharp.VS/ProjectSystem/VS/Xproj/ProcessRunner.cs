// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    /// <summary>
    /// Provides a simple wrapper for <see cref="System.Diagnostics.Process"/> to allow for testing.
    /// </summary>
    internal class ProcessRunner
    {
        [ImportingConstructor]
        public ProcessRunner() { }

        public Process Start(ProcessStartInfo info)
        {
            return Process.Start(info);
        }
    }
}
