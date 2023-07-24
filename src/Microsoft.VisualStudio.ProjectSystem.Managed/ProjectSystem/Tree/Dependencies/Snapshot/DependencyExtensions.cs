// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot;

internal static class DependencyExtensions
{
    public static DiagnosticLevel GetMaximumDiagnosticLevel(this ImmutableDictionary<DependencyGroupType, ImmutableArray<IDependency>> dependenciesByType)
    {
        DiagnosticLevel max = DiagnosticLevel.None;

        foreach ((_, ImmutableArray<IDependency> dependencies) in dependenciesByType)
        {
            foreach (IDependency dependency in dependencies)
            {
                if (dependency.DiagnosticLevel > max)
                {
                    max = dependency.DiagnosticLevel;
                }
            }
        }

        return max;
    }

    public static DiagnosticLevel GetDiagnosticLevel(this IImmutableDictionary<string, string> properties, DiagnosticLevel defaultLevel = DiagnosticLevel.None)
    {
        string? levelString = properties.GetStringProperty(ProjectItemMetadata.DiagnosticLevel);

        if (string.IsNullOrWhiteSpace(levelString))
        {
            return defaultLevel;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(levelString, "Warning"))
        {
            return DiagnosticLevel.Warning;
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(levelString, "Error"))
        {
            return DiagnosticLevel.Error;
        }

        return DiagnosticLevel.None;
    }
}
