// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(ICrossTargetRuleHandler<DependenciesRuleChangeContext>))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class PackageRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "NuGetDependency";

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.NuGetGrey,
            expandedIcon: ManagedImageMonikers.NuGetGrey,
            unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning);

        private static readonly SubTreeRootDependencyModel s_rootModel = new SubTreeRootDependencyModel(
            ProviderTypeString,
            VSResources.NuGetPackagesNodeName,
            s_iconSet,
            DependencyTreeFlags.NuGetSubTreeRootNodeFlags);

        [ImportingConstructor]
        public PackageRuleHandler(ITargetFrameworkProvider targetFrameworkProvider)
            : base(PackageReference.SchemaName, ResolvedPackageReference.SchemaName)
        {
            TargetFrameworkProvider = targetFrameworkProvider;
        }

        private ITargetFrameworkProvider TargetFrameworkProvider { get; }

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker GetImplicitIcon()
        {
            return ManagedImageMonikers.NuGetGreyPrivate;
        }

        public override void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            DependenciesRuleChangeContext ruleChangeContext)
        {
            var caseInsensitiveUnresolvedChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (changesByRuleName.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges))
            {
                caseInsensitiveUnresolvedChanges.AddRange(unresolvedChanges.After.Items.Keys);

                if (unresolvedChanges.Difference.AnyChanges)
                {
                    HandleChangesForRule(
                        unresolvedChanges,
                        ruleChangeContext,
                        targetFramework,
                        resolved: false);
                }
            }

            if (changesByRuleName.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges)
                && resolvedChanges.Difference.AnyChanges)
            {
                HandleChangesForRule(
                    resolvedChanges,
                    ruleChangeContext,
                    targetFramework,
                    resolved: true,
                    unresolvedChanges: caseInsensitiveUnresolvedChanges);
            }
        }

        private void HandleChangesForRule(
            IProjectChangeDescription projectChange,
            DependenciesRuleChangeContext ruleChangeContext,
            ITargetFramework targetFramework,
            bool resolved,
            HashSet<string> unresolvedChanges = null)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));

            if (targetFramework == null)
            {
                return;
            }

            foreach (string removedItem in projectChange.Difference.RemovedItems)
            {
                IImmutableDictionary<string, string> properties = projectChange.Before.GetProjectItemProperties(removedItem);
                IDependencyModel model = GetDependencyModel(removedItem, resolved,
                                            properties, unresolvedChanges, targetFramework);
                if (model == null)
                {
                    continue;
                }

                ruleChangeContext.IncludeRemovedChange(targetFramework, model);
            }

            foreach (string changedItem in projectChange.Difference.ChangedItems)
            {
                IImmutableDictionary<string, string> properties = projectChange.After.GetProjectItemProperties(changedItem);
                IDependencyModel model = GetDependencyModel(changedItem, resolved,
                                            properties, unresolvedChanges, targetFramework);
                if (model == null)
                {
                    continue;
                }

                ruleChangeContext.IncludeRemovedChange(targetFramework, model);
                ruleChangeContext.IncludeAddedChange(targetFramework, model);
            }

            foreach (string addedItem in projectChange.Difference.AddedItems)
            {
                IImmutableDictionary<string, string> properties = projectChange.After.GetProjectItemProperties(addedItem);
                IDependencyModel model = GetDependencyModel(addedItem, resolved,
                                            properties, unresolvedChanges, targetFramework);
                if (model == null)
                {
                    continue;
                }

                ruleChangeContext.IncludeAddedChange(targetFramework, model);
            }
        }

        private IDependencyModel GetDependencyModel(
            string itemSpec,
            bool resolved,
            IImmutableDictionary<string, string> properties,
            HashSet<string> unresolvedChanges,
            ITargetFramework targetFramework)
        {
            Requires.NotNull(itemSpec, nameof(itemSpec));
            Requires.NotNull(properties, nameof(properties));

            bool isTopLevel;

            string target = GetTargetFromDependencyId(itemSpec);

            DependencyType dependencyType = properties.GetEnumProperty<DependencyType>(ProjectItemMetadata.Type) ?? DependencyType.Unknown;
            string name = properties.GetStringProperty(ProjectItemMetadata.Name) ?? itemSpec;
            bool isImplicitlyDefined = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

            if (resolved)
            {
                isTopLevel = isImplicitlyDefined
                             || (dependencyType == DependencyType.Package
                                 && unresolvedChanges != null
                                 && unresolvedChanges.Contains(name));

                bool isTarget = itemSpec.IndexOf('/') == -1;

                if (isTarget)
                {
                    return null;
                }

                ITargetFramework packageTargetFramework = TargetFrameworkProvider.GetTargetFramework(target);

                if (packageTargetFramework?.Equals(targetFramework) != true)
                {
                    return null;
                }
            }
            else
            {
                isTopLevel = true;
            }

            string originalItemSpec = resolved && isTopLevel 
                ? name 
                : itemSpec;

            switch (dependencyType)
            {
                case DependencyType.Unknown when !resolved:
                case DependencyType.Package:
                    return new PackageDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        version: properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty,
                        resolved,
                        isImplicitlyDefined,
                        isTopLevel,
                        isVisible: !isImplicitlyDefined,
                        properties,
                        dependenciesIDs: GetDependencyItemSpecs());
                case DependencyType.Assembly:
                case DependencyType.FrameworkAssembly:
                    return new PackageAssemblyDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        dependenciesIDs: GetDependencyItemSpecs());
                case DependencyType.AnalyzerAssembly:
                    return new PackageAnalyzerAssemblyDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        dependenciesIDs: GetDependencyItemSpecs());
                case DependencyType.Diagnostic:
                    return new DiagnosticDependencyModel(
                        originalItemSpec,
                        severity: properties.GetEnumProperty<DiagnosticMessageSeverity>(ProjectItemMetadata.Severity) ?? DiagnosticMessageSeverity.Info,
                        code: properties.GetStringProperty(ProjectItemMetadata.DiagnosticCode) ?? string.Empty,
                        name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        isVisible: true,
                        properties: properties);
                default:
                    return new PackageUnknownDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        dependenciesIDs: GetDependencyItemSpecs());
            }

            string GetTargetFromDependencyId(string dependencyId)
            {
                var idParts = new LazyStringSplit(dependencyId, '/');

                string firstPart = idParts.FirstOrDefault();

                if (firstPart == null)
                {
                    // should never happen
                    throw new ArgumentException(nameof(dependencyId));
                }

                return firstPart;
            }

            IEnumerable<string> GetDependencyItemSpecs()
            {
                var dependenciesItemSpecs = new HashSet<string>(StringComparers.PropertyValues);
                if (properties.TryGetValue(ProjectItemMetadata.Dependencies, out string dependencies) && dependencies != null)
                {
                    var dependencyIds = new LazyStringSplit(dependencies, ';');

                    // store only unique dependency IDs
                    foreach (string dependencyId in dependencyIds)
                    {
                        dependenciesItemSpecs.Add($"{target}/{dependencyId}");
                    }
                }

                return dependenciesItemSpecs;
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;

        private enum DependencyType
        {
            Unknown,
            Target,
            Diagnostic,
            Package,
            Assembly,
            FrameworkAssembly,
            AnalyzerAssembly
        }
    }
}
