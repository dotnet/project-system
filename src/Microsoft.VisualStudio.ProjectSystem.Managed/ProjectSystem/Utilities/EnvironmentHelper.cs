// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// Wrapper over System.Environment abstraction for unit testing
    /// </summary>
    [Export(typeof(IEnvironmentHelper))]
    internal class EnvironmentHelper : IEnvironmentHelper
    {
        public string? GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public string ExpandEnvironmentVariables(string name)
        {
            if (name.IndexOf('%') == -1)
            {
                // There cannot be any environment variables in this string.
                // Avoid several allocations in the .NET Framework's implementation
                // of Environment.ExpandEnvironmentVariables.
                return name;
            }

            return Environment.ExpandEnvironmentVariables(name);
        }
    }
}
