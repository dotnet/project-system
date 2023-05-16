// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem;

internal static class ProjectConfigurationSliceExtensions
{
    /// <summary>
    /// Gets whether the specified slice's configuration matches the specified active project configuration.
    /// </summary>
    public static bool IsPrimaryActiveSlice(this ProjectConfigurationSlice slice, ProjectConfiguration activeProjectConfiguration)
    {
        // If all slice dimensions are present with the same value in the configuration, then this is a match.
        foreach ((string name, string value) in slice.Dimensions)
        {
            if (!activeProjectConfiguration.Dimensions.TryGetValue(name, out string activeValue) ||
                !StringComparers.ConfigurationDimensionValues.Equals(value, activeValue))
            {
                // The dimension's value is either unknown, or the value differs. This is not a match.
                return false;
            }
        }

        // All dimensions in the slice match the project configuration.
        // If the slice's configuration is empty, we also return true.
        return true;
    }
}
