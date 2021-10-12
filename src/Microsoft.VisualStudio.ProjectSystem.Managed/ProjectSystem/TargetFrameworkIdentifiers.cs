// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Contains the string constants for the most common target framework identifiers.
    ///     Should be compared using <see cref="VisualStudio.StringComparers.FrameworkIdentifiers"/>
    /// </summary>
    internal static class TargetFrameworkIdentifiers
    {
        // This is the most common identifiers, but it is not a complete list: i.e. .NETPortable.

        public const string NetCoreApp = ".NETCoreApp";

        public const string NetFramework = ".NETFramework";

        public const string NetStandard = ".NETStandard";
    }
}
