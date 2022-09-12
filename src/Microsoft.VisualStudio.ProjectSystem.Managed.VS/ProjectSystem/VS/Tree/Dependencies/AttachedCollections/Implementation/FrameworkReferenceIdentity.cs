// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    internal sealed class FrameworkReferenceIdentity
    {
        public string Path { get; }
        public string? Profile { get; }
        public string Name { get; }

        public FrameworkReferenceIdentity(string path, string? profile, string name)
        {
            Requires.NotNullOrWhiteSpace(path, nameof(path));
            Requires.NotNullOrWhiteSpace(name, nameof(name));

            Path = path;
            Profile = profile;
            Name = name;
        }

        private bool Equals(FrameworkReferenceIdentity other)
        {
            return Path == other.Path && Profile == other.Profile;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || (obj is FrameworkReferenceIdentity other && Equals(other));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Path.GetHashCode() * 397) ^ (Profile is not null ? Profile.GetHashCode() : 0);
            }
        }
    }
}
