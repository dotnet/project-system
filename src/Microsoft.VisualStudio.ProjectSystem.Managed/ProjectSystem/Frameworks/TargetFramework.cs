// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.Versioning;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class TargetFramework : ITargetFramework
    {
        public static readonly ITargetFramework Empty = new TargetFramework(string.Empty);

        /// <summary>
        /// The target framework used when a TFM short-name cannot be resolved.
        /// </summary>
        public static readonly ITargetFramework Unsupported = new TargetFramework("Unsupported,Version=v0.0");

        /// <summary>
        /// Any represents all TFMs, no need to be localized, used only in internal data.
        /// </summary>
        public static readonly ITargetFramework Any = new TargetFramework("any");

        public TargetFramework(FrameworkName frameworkName, string? shortName = null)
        {
            Requires.NotNull(frameworkName, nameof(frameworkName));

            FullName = frameworkName.FullName;
            ShortName = shortName ?? string.Empty;
        }

        /// <summary>
        /// Should be never used directly, this is for special cases or for some unknown framework,
        /// that Nuget is not aware of - highly unlikely to happen.
        /// </summary>
        /// <param name="moniker"></param>
        public TargetFramework(string moniker)
        {
            Requires.NotNull(moniker, nameof(moniker));

            FullName = moniker;
            ShortName = moniker;
        }

        public string FullName { get; }

        public string ShortName { get; }

        /// <summary>
        /// Override Equals to handle equivalency correctly. They are equal if the 
        /// monikers match
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ITargetFramework targetFramework && Equals(targetFramework);
        }

        public bool Equals(ITargetFramework? obj)
        {
            return obj != null && FullName.Equals(obj.FullName, StringComparisons.FrameworkIdentifiers);
        }

        public static bool operator ==(TargetFramework left, TargetFramework right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(TargetFramework left, TargetFramework right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return StringComparers.FrameworkIdentifiers.GetHashCode(FullName);
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
