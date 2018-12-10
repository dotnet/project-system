// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    internal sealed partial class PackageRuleHandler
    {
        private readonly struct PackageDependencyMetadata
        {
            private readonly DependencyType _dependencyType;

            public string Target { get; }
            public string ItemSpec { get; }
            public string OriginalItemSpec { get; }
            public string Name { get; }
            public bool IsResolved { get; }
            public bool IsImplicitlyDefined { get; }
            public bool IsTopLevel { get; }
            public IImmutableDictionary<string, string> Properties { get; }

            private PackageDependencyMetadata(
                DependencyType dependencyType,
                string target,
                string itemSpec,
                string originalItemSpec,
                string name,
                bool isResolved,
                bool isImplicitlyDefined,
                bool isTopLevel,
                IImmutableDictionary<string, string> properties)
            {
                _dependencyType = dependencyType;
                Target = target;
                ItemSpec = itemSpec;
                OriginalItemSpec = originalItemSpec;
                Name = name;
                IsResolved = isResolved;
                IsImplicitlyDefined = isImplicitlyDefined;
                IsTopLevel = isTopLevel;
                Properties = properties;
            }

            public static bool TryGetMetadata(
                string itemSpec,
                bool isResolved,
                IImmutableDictionary<string, string> properties,
                HashSet<string> unresolvedChanges,
                ITargetFramework targetFramework,
                ITargetFrameworkProvider targetFrameworkProvider,
                out PackageDependencyMetadata metadata)
            {
                Requires.NotNull(itemSpec, nameof(itemSpec));
                Requires.NotNull(properties, nameof(properties));
                // unresolvedChanges can be null
                Requires.NotNull(targetFramework, nameof(targetFramework));
                Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));

                bool isTopLevel;

                string target = GetTargetFromDependencyId(itemSpec);

                DependencyType dependencyType = properties.GetEnumProperty<DependencyType>(ProjectItemMetadata.Type)
                    ?? (isResolved ? DependencyType.Unknown : DependencyType.Package);

                string name = properties.GetStringProperty(ProjectItemMetadata.Name) ?? itemSpec;

                bool isImplicitlyDefined = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

                if (isResolved)
                {
                    isTopLevel = isImplicitlyDefined ||
                        (dependencyType == DependencyType.Package && unresolvedChanges?.Contains(name) == true);

                    bool isTarget = itemSpec.IndexOf('/') == -1;

                    if (isTarget)
                    {
                        metadata = default;
                        return false;
                    }

                    ITargetFramework packageTargetFramework = targetFrameworkProvider.GetTargetFramework(target);

                    if (packageTargetFramework?.Equals(targetFramework) != true)
                    {
                        metadata = default;
                        return false;
                    }
                }
                else
                {
                    isTopLevel = true;
                }

                string originalItemSpec = isResolved && isTopLevel
                    ? name
                    : itemSpec;

                metadata = new PackageDependencyMetadata(
                    dependencyType,
                    target,
                    itemSpec,
                    originalItemSpec,
                    name,
                    isResolved,
                    isImplicitlyDefined,
                    isTopLevel,
                    properties);
                return true;

                string GetTargetFromDependencyId(string dependencyId)
                {
                    string firstPart = new LazyStringSplit(dependencyId, '/').FirstOrDefault();

                    if (firstPart == null)
                    {
                        // should never happen
                        throw new ArgumentException(nameof(dependencyId));
                    }

                    return firstPart;
                }
            }

            public IDependencyModel CreateDependencyModel()
            {
                switch (_dependencyType)
                {
                    case DependencyType.Package:
                        return new PackageDependencyModel(
                            ItemSpec,
                            OriginalItemSpec,
                            Name,
                            version: Properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty,
                            IsResolved,
                            IsImplicitlyDefined,
                            IsTopLevel,
                            isVisible: !IsImplicitlyDefined,
                            Properties,
                            dependenciesIDs: GetDependencyItemSpecs());
                    case DependencyType.Assembly:
                    case DependencyType.FrameworkAssembly:
                        return new PackageAssemblyDependencyModel(
                            ItemSpec,
                            OriginalItemSpec,
                            Name,
                            IsResolved,
                            Properties,
                            dependenciesIDs: GetDependencyItemSpecs());
                    case DependencyType.AnalyzerAssembly:
                        return new PackageAnalyzerAssemblyDependencyModel(
                            ItemSpec,
                            OriginalItemSpec,
                            Name,
                            IsResolved,
                            Properties,
                            dependenciesIDs: GetDependencyItemSpecs());
                    case DependencyType.Diagnostic:
                        return new DiagnosticDependencyModel(
                            OriginalItemSpec,
                            severity: Properties.GetEnumProperty<DiagnosticMessageSeverity>(ProjectItemMetadata.Severity) ?? DiagnosticMessageSeverity.Info,
                            code: Properties.GetStringProperty(ProjectItemMetadata.DiagnosticCode) ?? string.Empty,
                            Name,
                            isVisible: true,
                            properties: Properties);
                    default:
                        return new PackageUnknownDependencyModel(
                            ItemSpec,
                            OriginalItemSpec,
                            Name,
                            IsResolved,
                            Properties,
                            dependenciesIDs: GetDependencyItemSpecs());
                }
            }

            private IEnumerable<string> GetDependencyItemSpecs()
            {
                if (Properties.TryGetValue(ProjectItemMetadata.Dependencies, out string dependencies) && !string.IsNullOrWhiteSpace(dependencies))
                {
                    var dependenciesItemSpecs = new HashSet<string>(StringComparers.PropertyValues);
                    var dependencyIds = new LazyStringSplit(dependencies, ';');

                    // store only unique dependency IDs
                    foreach (string dependencyId in dependencyIds)
                    {
                        dependenciesItemSpecs.Add($"{Target}/{dependencyId}");
                    }

                    return dependenciesItemSpecs;
                }

                return Array.Empty<string>();
            }

            private enum DependencyType
            {
                Unknown,
                Diagnostic,
                Package,
                Assembly,
                FrameworkAssembly,
                AnalyzerAssembly
            }
        }
    }
}
