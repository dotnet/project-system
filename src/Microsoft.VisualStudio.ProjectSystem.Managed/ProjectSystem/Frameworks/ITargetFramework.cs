// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.Versioning;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal interface ITargetFramework : IEquatable<ITargetFramework?>,
                                          IEquatable<string?>
    {
        /// <summary>
        /// Basic target framework info. Can be <see langword="null" /> if the framework is unknown.
        /// </summary>
        FrameworkName? FrameworkName { get; }

        /// <summary>
        /// Gets the full moniker (TFM).
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        string ShortName { get; }

        /// <summary>
        /// Gets the display name. Can be an empty string.
        /// </summary>
        string FriendlyName { get; }
    }
}
