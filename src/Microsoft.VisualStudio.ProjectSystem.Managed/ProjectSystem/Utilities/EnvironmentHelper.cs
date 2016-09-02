// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Wrapper over System.Environment abstraction for unit testing
    /// </summary>
    [Export(typeof(IEnvironmentHelper))]
    internal class EnvironmentHelper : IEnvironmentHelper
    {
        public string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public string ExpandEnvironmentVariables(string name)
        {
            return Environment.ExpandEnvironmentVariables(name);
        }

        public bool Is64BitOperatingSystem
        {
            get
            {
                return Environment.Is64BitOperatingSystem;
            }
        }
    }
}
