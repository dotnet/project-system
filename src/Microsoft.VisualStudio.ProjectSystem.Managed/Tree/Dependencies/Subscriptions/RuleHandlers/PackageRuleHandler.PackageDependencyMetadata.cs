// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    internal sealed partial class PackageRuleHandler
    {
        private readonly struct PackageDependencyMetadata
        {
            private static readonly InternPool<string> s_targetFrameworkInternPool = new InternPool<string>(StringComparer.Ordinal);

            private readonly DependencyType _dependencyType;

            public string? TargetFrameworkName { get; }
            public string ItemSpec { get; }
            public string OriginalItemSpec { get; }
            public string Name { get; }
            public bool IsResolved { get; }
            public bool IsImplicitlyDefined { get; }
            public bool IsTopLevel { get; }
            public IImmutableDictionary<string, string> Properties { get; }

            private PackageDependencyMetadata(
                DependencyType dependencyType,
                string? targetFrameworkName,
                string itemSpec,
                string originalItemSpec,
                string name,
                bool isResolved,
                bool isImplicitlyDefined,
                bool isTopLevel,
                IImmutableDictionary<string, string> properties)
            {
                _dependencyType = dependencyType;
                TargetFrameworkName = targetFrameworkName;
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
                Func<string, bool>? isEvaluatedItemSpec,
                ITargetFramework targetFramework,
                ITargetFrameworkProvider targetFrameworkProvider,
                out PackageDependencyMetadata metadata)
            {
                Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));
                Requires.NotNull(properties, nameof(properties));

                bool isImplicitlyDefined = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

                if (isResolved)
                {
                    // We have design-time build data

                    Requires.NotNull(targetFramework, nameof(targetFramework));
                    Requires.NotNull(targetFrameworkProvider, nameof(targetFrameworkProvider));
                    Requires.NotNull(isEvaluatedItemSpec!, nameof(isEvaluatedItemSpec));

                    DependencyType dependencyType = properties.GetEnumProperty<DependencyType>(ProjectItemMetadata.Type) ?? DependencyType.Unknown;

                    if (dependencyType == DependencyType.Target)
                    {
                        // Disregard items of type 'Target' from design-time build
                        metadata = default;
                        return false;
                    }

                    int slashIndex = itemSpec.IndexOf('/');
                    string? targetFrameworkName = slashIndex == -1 ? null : s_targetFrameworkInternPool.Intern(itemSpec.Substring(0, slashIndex));

                    if (targetFrameworkName == null ||
                        targetFrameworkProvider.GetTargetFramework(targetFrameworkName)?.Equals(targetFramework) != true)
                    {
                        metadata = default;
                        return false;
                    }

                    string name = properties.GetStringProperty(ProjectItemMetadata.Name) ?? itemSpec;

                    bool isTopLevel = isImplicitlyDefined ||
                        (dependencyType == DependencyType.Package && isEvaluatedItemSpec(name));

                    string originalItemSpec = isTopLevel ? name : itemSpec;

                    metadata = new PackageDependencyMetadata(
                        dependencyType,
                        targetFrameworkName,
                        itemSpec,
                        originalItemSpec,
                        name,
                        isResolved: true,
                        isImplicitlyDefined,
                        isTopLevel,
                        properties);
                }
                else
                {
                    // We only have evaluation data

                    System.Diagnostics.Debug.Assert(itemSpec.IndexOf('/') == -1);

                    metadata = new PackageDependencyMetadata(
                        dependencyType: DependencyType.Package,
                        targetFrameworkName: null,
                        itemSpec,
                        originalItemSpec: itemSpec,
                        name: itemSpec,
                        isResolved: false,
                        isImplicitlyDefined,
                        isTopLevel: true,
                        properties);
                }

                return true;
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
                    Assumes.NotNull(TargetFrameworkName);

                    var dependenciesItemSpecs = new HashSet<string>(StringComparers.ItemNames);
                    var dependencyIds = new LazyStringSplit(dependencies, ';');

                    // store only unique dependency IDs
                    foreach (string dependencyId in dependencyIds)
                    {
                        dependenciesItemSpecs.Add($"{TargetFrameworkName}/{dependencyId}");
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
                Target,
                FrameworkAssembly,
                AnalyzerAssembly
            }
        }
    }
}
