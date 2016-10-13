// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class ProjectConfigurationExtensions
    {
        /// <summary>
        /// Returns true if this is a cross-targeting project configuration with a "TargetFramework" dimension.
        /// This is true for a project with an explicit "TargetFrameworks" property, but no "TargetFrameworkMoniker" or "TargetFramework" property.
        /// </summary>
        /// <param name="projectConfiguration"></param>
        /// <returns></returns>
        internal static bool IsCrossTargeting(this ProjectConfiguration projectConfiguration)
        {
            return projectConfiguration.Dimensions.ContainsKey(TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName);
        }

        /// <summary>
        /// Returns true if the given project configurations are equal ignoring the "TargetFramework" dimension.
        /// </summary>
        internal static bool EqualIgnoringTargetFramework(this ProjectConfiguration projectConfiguration1, ProjectConfiguration projectConfiguration2)
        {
            Requires.NotNull(projectConfiguration1, nameof(projectConfiguration1));
            Requires.NotNull(projectConfiguration2, nameof(projectConfiguration2));

            if (projectConfiguration1.Dimensions.Count != projectConfiguration2.Dimensions.Count)
            {
                return false;
            }

            if (!projectConfiguration1.IsCrossTargeting() || !projectConfiguration2.IsCrossTargeting())
            {
                return projectConfiguration1.Equals(projectConfiguration2);
            }

            foreach (var dimensionKvp in projectConfiguration1.Dimensions)
            {
                var dimensionName = dimensionKvp.Key;
                var dimensionValue = dimensionKvp.Value;

                // Ignore the TargetFramework.
                if (string.Equals(dimensionName, TargetFrameworkProjectConfigurationDimensionProvider.TargetFrameworkPropertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string activeValue;
                if (!projectConfiguration2.Dimensions.TryGetValue(dimensionName, out activeValue) ||
                    !string.Equals(dimensionValue, activeValue, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
