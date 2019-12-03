// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.Versioning;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class TargetFramework : ITargetFramework
    {
        public static readonly ITargetFramework Empty = new TargetFramework(string.Empty);

        /// <summary>
        /// Any represents all TFMs, no need to be localized, used only in internal data.
        /// </summary>
        public static readonly ITargetFramework Any = new TargetFramework("any");

        public TargetFramework(FrameworkName frameworkName, string? shortName = null)
        {
            Requires.NotNull(frameworkName, nameof(frameworkName));

            FrameworkName = frameworkName;
            FullName = frameworkName.FullName;
            ShortName = shortName ?? string.Empty;
            FriendlyName = $"{frameworkName.Identifier} {frameworkName.Version}";
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
            FriendlyName = moniker;
        }

        public FrameworkName? FrameworkName { get; }

        public string FullName { get; }

        public string ShortName { get; }

        public string FriendlyName { get; }

        /// <summary>
        /// Override Equals to handle equivalency correctly. They are equal if the 
        /// monikers match
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj switch
            {
                ITargetFramework targetFramework => Equals(targetFramework),
                string s => Equals(s),
                _ => false
            };
        }

        public bool Equals(ITargetFramework? obj)
        {
            if (obj != null)
            {
                return FullName.Equals(obj.FullName, StringComparisons.FrameworkIdentifiers);
            }

            return false;
        }

        public bool Equals(string? obj)
        {
            if (obj != null)
            {
                return string.Equals(FullName, obj, StringComparisons.FrameworkIdentifiers)
                    || string.Equals(ShortName, obj, StringComparisons.FrameworkIdentifiers);
            }

            return false;
        }

        public static bool operator ==(TargetFramework left, TargetFramework right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(TargetFramework left, TargetFramework right)
            => !(left == right);

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
