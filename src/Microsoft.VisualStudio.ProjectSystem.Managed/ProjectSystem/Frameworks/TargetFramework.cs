// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class TargetFramework : IEquatable<TargetFramework?>
    {
        public static readonly TargetFramework Empty = new(string.Empty);

        /// <summary>
        /// The target framework used when a TFM short-name cannot be resolved.
        /// </summary>
        public static readonly TargetFramework Unsupported = new("Unsupported,Version=v0.0");

        /// <summary>
        /// Any represents all TFMs, no need to be localized, used only in internal data.
        /// </summary>
        public static readonly TargetFramework Any = new("any");

        public TargetFramework(string moniker)
        {
            Requires.NotNull(moniker, nameof(moniker));

            TargetFrameworkMoniker = moniker;
        }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        public string TargetFrameworkMoniker { get; }

        /// <summary>
        /// Override Equals to handle equivalency correctly. They are equal if the
        /// monikers match
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is TargetFramework targetFramework && Equals(targetFramework);
        }

        public bool Equals(TargetFramework? obj)
        {
            return obj != null && TargetFrameworkMoniker.Equals(obj.TargetFrameworkMoniker, StringComparisons.FrameworkIdentifiers);
        }

        public static bool operator ==(TargetFramework? left, TargetFramework? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(TargetFramework? left, TargetFramework? right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return StringComparers.FrameworkIdentifiers.GetHashCode(TargetFrameworkMoniker);
        }

        public override string ToString()
        {
            return TargetFrameworkMoniker;
        }
    }
}
