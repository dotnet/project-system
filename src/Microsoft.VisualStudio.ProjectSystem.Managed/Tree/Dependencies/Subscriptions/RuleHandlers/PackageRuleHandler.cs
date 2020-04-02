// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class PackageRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "NuGetDependency";

        private static readonly DependencyGroupModel s_groupModel = new DependencyGroupModel(
            ProviderTypeString,
            Resources.PackagesNodeName,
            new DependencyIconSet(
                icon: ManagedImageMonikers.NuGetGrey,
                expandedIcon: ManagedImageMonikers.NuGetGrey,
                unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
                unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning),
            DependencyTreeFlags.PackageDependencyGroup);

        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public PackageRuleHandler(ITargetFrameworkProvider targetFrameworkProvider)
            : base(PackageReference.SchemaName, ResolvedPackageReference.SchemaName)
        {
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public override string ProviderType => ProviderTypeString;

        public override ImageMoniker ImplicitIcon => ManagedImageMonikers.NuGetGreyPrivate;

        protected override void HandleAddedItem(
            string addedItem,
            bool resolved,
            IProjectChangeDescription projectChange,
            DependenciesChangesBuilder changesBuilder,
            ITargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            if (TryCreatePackageDependencyModel(
                addedItem,
                resolved,
                properties: projectChange.After.GetProjectItemProperties(addedItem)!,
                isEvaluatedItemSpec,
                targetFramework,
                _targetFrameworkProvider,
                out PackageDependencyModel? dependencyModel))
            {
                changesBuilder.Added(dependencyModel);
            }
        }

        protected override void HandleChangedItem(
            string changedItem,
            bool resolved,
            IProjectChangeDescription projectChange,
            DependenciesChangesBuilder changesBuilder,
            ITargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            if (TryCreatePackageDependencyModel(
                changedItem,
                resolved,
                properties: projectChange.After.GetProjectItemProperties(changedItem)!,
                isEvaluatedItemSpec,
                targetFramework,
                _targetFrameworkProvider,
                out PackageDependencyModel? dependencyModel))
            {
                changesBuilder.Removed(ProviderTypeString, dependencyModel.OriginalItemSpec);
                changesBuilder.Added(dependencyModel);
            }
        }

        protected override void HandleRemovedItem(
            string removedItem,
            bool resolved,
            IProjectChangeDescription projectChange,
            DependenciesChangesBuilder changesBuilder,
            ITargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            if (TryCreatePackageDependencyModel(
                removedItem,
                resolved,
                properties: projectChange.Before.GetProjectItemProperties(removedItem)!,
                isEvaluatedItemSpec,
                targetFramework,
                _targetFrameworkProvider,
                out PackageDependencyModel? dependencyModel))
            {
                changesBuilder.Removed(ProviderTypeString, dependencyModel.OriginalItemSpec);
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        private static readonly InternPool<string> s_targetFrameworkInternPool = new InternPool<string>(StringComparer.Ordinal);

        private static bool TryCreatePackageDependencyModel(
            string itemSpec,
            bool isResolved,
            IImmutableDictionary<string, string> properties,
            Func<string, bool>? isEvaluatedItemSpec,
            ITargetFramework targetFramework,
            ITargetFrameworkProvider targetFrameworkProvider,
            [NotNullWhen(returnValue: true)] out PackageDependencyModel? dependencyModel)
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

                string? dependencyType = properties.GetStringProperty(ProjectItemMetadata.Type);

                if (!StringComparers.PropertyLiteralValues.Equals(dependencyType, "package"))
                {
                    // Other types handled via assets file
                    // TODO filter in DTB targets and remove the 'Type' metadata altogether
                    dependencyModel = default;
                    return false;
                }

                // Before 16.7 (SDK 3.1.4xx) ResolvedPackageReference items (from PreprocessPackageDependenciesDesignTime) would return
                // packages for all targets in a DTB, where the target could be identified by the ItemSpec (for example:
                // ".NETFramework,Version=v4.8/MetadataExtractor/2.2.0"). We would then filter the ones we weren't interested in.
                // From 16.7 we no longer return items from other target frameworks during DTB, and we remove the target prefix from ItemSpec.
                // In order to not break scenarios where 16.7+ runs using an older SDK, we need to identify which ItemSpec format is used,
                // and maintain filtering logic.
                int slashIndex = itemSpec.IndexOf('/');
                if (slashIndex != -1 && itemSpec.IndexOf('/', slashIndex + 1) != -1)
                {
                    // ItemSpec contains more than one '/'. It's the old format and we must apply filtering.
                    string targetFrameworkName = s_targetFrameworkInternPool.Intern(itemSpec.Substring(0, slashIndex));

                    if (targetFrameworkProvider.GetTargetFramework(targetFrameworkName)?.Equals(targetFramework) != true)
                    {
                        dependencyModel = default;
                        return false;
                    }
                }

                string name = properties.GetStringProperty(ProjectItemMetadata.Name) ?? itemSpec;

                bool isTopLevel = isImplicitlyDefined || isEvaluatedItemSpec(name);

                if (!isTopLevel)
                {
                    dependencyModel = default;
                    return false;
                }

                string originalItemSpec = isTopLevel ? name : itemSpec;

                dependencyModel = new PackageDependencyModel(
                    itemSpec,
                    originalItemSpec,
                    name,
                    version: properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty,
                    isResolved: true,
                    isImplicitlyDefined,
                    isVisible: !isImplicitlyDefined,
                    properties);
            }
            else
            {
                // We only have evaluation data

                System.Diagnostics.Debug.Assert(itemSpec.IndexOf('/') == -1);

                dependencyModel = new PackageDependencyModel(
                    itemSpec,
                    originalItemSpec: itemSpec,
                    name: itemSpec,
                    version: properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty,
                    isResolved: false,
                    isImplicitlyDefined,
                    isVisible: !isImplicitlyDefined,
                    properties);
            }

            return true;
        }
    }
}
