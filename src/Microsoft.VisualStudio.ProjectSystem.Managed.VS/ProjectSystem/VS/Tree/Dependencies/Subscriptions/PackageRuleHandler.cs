// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

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
            if (changesByRuleName.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges)
                && unresolvedChanges.Difference.AnyChanges)
            {
                HandleChangesForRule(
                    unresolvedChanges,
                    ruleChangeContext,
                    targetFramework,
                    resolved: false);
            }

            var caseInsensitiveUnresolvedChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            caseInsensitiveUnresolvedChanges.AddRange(unresolvedChanges.After.Items.Keys);

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
                IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectChange.Before, removedItem);
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
                IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectChange.After, changedItem);
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
                IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectChange.After, addedItem);
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
            PackageDependencyMetadata metadata;
            bool isTopLevel = true;
            bool isTarget = false;
            if (resolved)
            {
                metadata = new PackageDependencyMetadata(itemSpec, properties);
                isTopLevel = metadata.IsImplicitlyDefined
                             || (metadata.DependencyType == DependencyType.Package
                                 && unresolvedChanges != null
                                 && unresolvedChanges.Contains(metadata.Name));
                isTarget = metadata.IsTarget;
                ITargetFramework packageTargetFramework = TargetFrameworkProvider.GetTargetFramework(metadata.Target);
                if (packageTargetFramework?.Equals(targetFramework) != true)
                {
                    return null;
                }
            }
            else
            {
                metadata = CreateUnresolvedMetadata(itemSpec, properties);
            }

            if (isTarget)
            {
                return null;
            }

            string originalItemSpec = itemSpec;
            if (resolved && isTopLevel)
            {
                originalItemSpec = metadata.Name;
            }

            switch (metadata.DependencyType)
            {
                case DependencyType.Package:
                    return new PackageDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        metadata.Version,
                        resolved,
                        metadata.IsImplicitlyDefined,
                        isTopLevel,
                        isVisible: !metadata.IsImplicitlyDefined,
                        properties,
                        metadata.DependenciesItemSpecs);
                case DependencyType.Assembly:
                case DependencyType.FrameworkAssembly:
                    return new PackageAssemblyDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        metadata.DependenciesItemSpecs);
                case DependencyType.AnalyzerAssembly:
                    return new PackageAnalyzerAssemblyDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        metadata.DependenciesItemSpecs);
                case DependencyType.Diagnostic:
                    return new DiagnosticDependencyModel(
                        itemSpec,
                        metadata.Severity,
                        metadata.DiagnosticCode,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        isVisible: true,
                        properties: properties);
                default:
                    return new PackageUnknownDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        metadata.Name,
                        DependencyTreeFlags.NuGetSubTreeNodeFlags,
                        resolved,
                        properties,
                        metadata.DependenciesItemSpecs);
            }
        }

        public override IDependencyModel CreateRootDependencyNode()
        {
            return new SubTreeRootDependencyModel(
                ProviderType,
                VSResources.NuGetPackagesNodeName,
                s_iconSet,
                DependencyTreeFlags.NuGetSubTreeRootNodeFlags);
        }

        private static PackageDependencyMetadata CreateUnresolvedMetadata(
            string itemSpec,
            IImmutableDictionary<string, string> properties)
        {
            // add this properties here since unresolved PackageReferences don't have it
            properties = properties.SetItem(ProjectItemMetadata.Resolved, "false");
            properties = properties.SetItem(ProjectItemMetadata.Type, DependencyType.Package.ToString());

            return new PackageDependencyMetadata(itemSpec, properties);
        }

        protected class PackageDependencyMetadata
        {
            public PackageDependencyMetadata(string itemSpec, IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(itemSpec, nameof(itemSpec));

                ItemSpec = itemSpec;
                Target = GetTargetFromDependencyId(ItemSpec);

                SetProperties(properties);
            }

            public string Name { get; private set; }
            public string Version { get; private set; }
            public DependencyType DependencyType { get; private set; }
            public string Path { get; private set; }
            public bool Resolved { get; private set; }
            public string ItemSpec { get; set; }
            public string Target { get; }
            public bool IsTarget
            {
                get
                {
                    return !ItemSpec.Contains("/");
                }
            }

            public bool IsImplicitlyDefined { get; private set; }

            public IImmutableDictionary<string, string> Properties { get; set; }

            public HashSet<string> DependenciesItemSpecs { get; private set; }

            public DiagnosticMessageSeverity Severity { get; private set; }
            public string DiagnosticCode { get; private set; }

            public void SetProperties(IImmutableDictionary<string, string> properties)
            {
                Requires.NotNull(properties, nameof(properties));
                Properties = properties;

                DependencyType = GetEnumMetadata<DependencyType>(ProjectItemMetadata.Type) ?? DependencyType.Unknown;
                Name = GetStringMetadata(ProjectItemMetadata.Name);
                if (string.IsNullOrEmpty(Name))
                {
                    Name = ItemSpec;
                }

                Version = GetStringMetadata(ProjectItemMetadata.Version);
                Path = GetStringMetadata(ProjectItemMetadata.Path);
                Resolved = GetBoolMetadata(ProjectItemMetadata.Resolved) ?? true;
                IsImplicitlyDefined = GetBoolMetadata(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

                var dependenciesHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (properties.TryGetValue(ProjectItemMetadata.Dependencies, out string dependencies) && dependencies != null)
                {
                    string[] dependencyIds = dependencies.Split(Delimiter.Semicolon, StringSplitOptions.RemoveEmptyEntries);

                    // store only unique dependency IDs
                    foreach (string dependencyId in dependencyIds)
                    {
                        dependenciesHashSet.Add($"{Target}/{dependencyId}");
                    }
                }

                DependenciesItemSpecs = dependenciesHashSet;

                if (DependencyType == DependencyType.Diagnostic)
                {
                    Severity = GetEnumMetadata<DiagnosticMessageSeverity>(ProjectItemMetadata.Severity) ?? DiagnosticMessageSeverity.Info;
                    DiagnosticCode = GetStringMetadata(ProjectItemMetadata.DiagnosticCode);
                }
            }

            private string GetStringMetadata(string metadataName)
            {
                if (Properties.TryGetValue(metadataName, out string value))
                {
                    return value;
                }

                return string.Empty;
            }

            private T? GetEnumMetadata<T>(string metadataName) where T : struct
            {
                string enumString = GetStringMetadata(metadataName);
                return Enum.TryParse(enumString, ignoreCase: true, out T enumValue) ? enumValue : (T?)null;
            }

            private bool? GetBoolMetadata(string metadataName)
            {
                string boolString = GetStringMetadata(metadataName);
                return bool.TryParse(boolString, out bool boolValue) ? boolValue : (bool?)null;
            }

            public static string GetTargetFromDependencyId(string dependencyId)
            {
                string[] idParts = dependencyId.Split(Delimiter.ForwardSlash, StringSplitOptions.RemoveEmptyEntries);
                Requires.NotNull(idParts, nameof(idParts));
                if (idParts.Length == 0)
                {
                    // should never happen
                    throw new ArgumentException(nameof(idParts));
                }

                return idParts[0];
            }
        }

        protected enum DependencyType
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
