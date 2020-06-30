// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal interface ITargetFramework : IEquatable<ITargetFramework?>,
                                          IEquatable<string?>
    {
        /// <summary>
        /// Gets the full moniker (TFM).
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        string ShortName { get; }
    }
}
