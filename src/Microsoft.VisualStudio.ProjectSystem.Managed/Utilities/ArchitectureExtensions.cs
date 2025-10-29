// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio;

internal static class ArchitectureExtensions
{
    /// <summary>
    /// Returns the architecture as a lowercase string commonly used in file paths and identifiers.
    /// This extension avoids a string allocation for known architectures.
    /// </summary>
    /// <param name="architecture"></param>
    /// <returns>The lowercase version of the architecture.</returns>
    public static string GetArchitectureString(this Architecture architecture)
    {
        return architecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => architecture.ToString().ToLower()
        };
    }
}
