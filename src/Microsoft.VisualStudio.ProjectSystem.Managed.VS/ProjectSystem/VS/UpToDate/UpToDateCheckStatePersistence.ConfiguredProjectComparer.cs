// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate
{
    internal sealed partial class UpToDateCheckStatePersistence
    {
        /// <summary>
        /// Compares projects and dimensions.
        /// </summary>
        private sealed class ConfiguredProjectComparer : IEqualityComparer<(string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions)>
        {
            public static ConfiguredProjectComparer Instance { get; } = new();

            public bool Equals(
                (string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions) x,
                (string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions) y)
            {
                if (!StringComparers.Paths.Equals(x.ProjectPath, y.ProjectPath))
                    return false;

                if (x.ConfigurationDimensions.Count != y.ConfigurationDimensions.Count)
                    return false;

                foreach ((string name, string xValue) in x.ConfigurationDimensions)
                {
                    if (!y.ConfigurationDimensions.TryGetValue(name, out string? yValue) ||
                        !StringComparers.ConfigurationDimensionValues.Equals(xValue, yValue))
                        return false;
                }

                return true;
            }

            public int GetHashCode((string ProjectPath, IImmutableDictionary<string, string> ConfigurationDimensions) obj)
            {
                unchecked
                {
                    int hash = obj.ProjectPath.GetHashCode();

                    foreach ((string name, string value) in obj.ConfigurationDimensions)
                    {
                        // XOR values so that order doesn't matter
                        hash ^= (name.GetHashCode() * 397) ^ value.GetHashCode();
                    }

                    return hash;
                }
            }
        }
    }
}
