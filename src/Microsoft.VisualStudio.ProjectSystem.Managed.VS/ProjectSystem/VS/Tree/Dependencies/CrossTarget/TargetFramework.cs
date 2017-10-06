// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.Versioning;
using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal class TargetFramework : ITargetFramework
    {
        public static ITargetFramework Empty = new TargetFramework(string.Empty);

        /// <summary>
        /// Any represents all TFMs, no need to be localized, used only in internal data.
        /// </summary>
        public static ITargetFramework Any = new TargetFramework("any");

        public TargetFramework(FrameworkName frameworkName, string shortName = null)
        {
            Requires.NotNull(frameworkName, nameof(frameworkName));

            FrameworkName = frameworkName;
            Moniker = frameworkName.FullName;
            ShortName = shortName ?? string.Empty;
            FriendlyName = $"{frameworkName.Identifier} {Version}";
        }

        /// <summary>
        /// Should be never used directly, this is for special cases or for some unknown framework,
        /// that Nuget is not aware of - highly unlikely to happen.
        /// </summary>
        /// <param name="moniker"></param>
        public TargetFramework(string moniker)
        {
            Requires.NotNull(moniker, nameof(moniker));

            Moniker = moniker;
            ShortName = moniker;
            FriendlyName = moniker;
        }

        public FrameworkName FrameworkName { get; }

        /// <summary>
        /// Gets the full moniker (TFM).
        /// </summary>
        public string Moniker { get; }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string FriendlyName { get; }

        public string Version
        {
            get
            {
                return FrameworkName?.Version.ToString();
            }
        }

        /// <summary>
        /// Override Equals to handle equivalency correctly. They are equal if the 
        /// monikers match
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is ITargetFramework targetFramework)
            {
                return Equals(targetFramework);
            }
            else
            {
                return Equals(obj as string);
            }
        }

        public bool Equals(ITargetFramework obj)
        {
            if (obj != null)
            {
                return Moniker.Equals(obj.Moniker, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public bool Equals(string obj)
        {
            if (obj != null)
            {
                return string.Equals(Moniker, obj, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ShortName, obj, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        ///  Need to override this to ensure it can be hashed correctly
        /// </summary>
        public override int GetHashCode()
        {
            if (Moniker == null)
            {
                return string.Empty.GetHashCode();
            }
            else
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(Moniker);
            }
        }

        public int CompareTo(ITargetFramework other)
        {
            if (other == null)
            {
                return 1;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(Moniker, other.Moniker);
        }

        public override string ToString()
        {
            return Moniker ?? string.Empty;
        }
    }
}
