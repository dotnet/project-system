// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.NuGet
{
    /// <summary>
    /// Copied from NuGet, where the type is internal.
    /// </summary>
    internal static class NuGetUtils
    {
        public static bool IsPlaceholderFile(string path)
        {
            if (path.EndsWith("_._", StringComparison.Ordinal))
            {
                if (path.Length == 3)
                {
                    return true;
                }

                char separator = path[path.Length - 4];
                return separator == '\\' || separator == '/';
            }

            return false;
        }
    }
}
