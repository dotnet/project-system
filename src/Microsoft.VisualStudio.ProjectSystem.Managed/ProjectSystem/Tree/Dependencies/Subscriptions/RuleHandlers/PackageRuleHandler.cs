// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed class PackageRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "Package";

        private static readonly DependencyGroupModel s_groupModel = new(
            ProviderTypeString,
            Resources.PackagesNodeName,
            new DependencyIconSet(
                icon: KnownMonikers.NuGetNoColor,
                expandedIcon: KnownMonikers.NuGetNoColor,
                unresolvedIcon: KnownMonikers.NuGetNoColorWarning,
                unresolvedExpandedIcon: KnownMonikers.NuGetNoColorWarning,
                implicitIcon: KnownMonikers.NuGetNoColorPrivate,
                implicitExpandedIcon: KnownMonikers.NuGetNoColorPrivate),
            DependencyTreeFlags.PackageDependencyGroup);

        private readonly ITargetFrameworkProvider _targetFrameworkProvider;

        [ImportingConstructor]
        public PackageRuleHandler(ITargetFrameworkProvider targetFrameworkProvider)
            : base(PackageReference.SchemaName, ResolvedPackageReference.SchemaName)
        {
            _targetFrameworkProvider = targetFrameworkProvider;
        }

        public override string ProviderType => ProviderTypeString;

        public override ProjectTreeFlags GroupNodeFlag => DependencyTreeFlags.PackageDependencyGroup;

        protected override void HandleAddedItem(
            string projectFullPath,
            string addedItem,
            bool isResolvedItem,
            IProjectChangeDescription projectChange,
            IProjectRuleSnapshot evaluationRuleSnapshot,
            DependenciesChangesBuilder changesBuilder,
            TargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            if (TryCreatePackageDependencyModel(
                projectFullPath,
                addedItem,
                isResolvedItem,
                properties: projectChange.After.GetProjectItemProperties(addedItem)!,
                evaluationRuleSnapshot,
                isEvaluatedItemSpec,
                targetFramework,
                changesBuilder,
                out PackageDependencyModel? dependencyModel))
            {
                changesBuilder.Added(targetFramework, dependencyModel);
            }
        }

        protected override void HandleRemovedItem(
            string projectFullPath,
            string removedItem,
            bool isResolvedItem,
            IProjectChangeDescription projectChange,
            IProjectRuleSnapshot evaluationRuleSnapshot,
            DependenciesChangesBuilder changesBuilder,
            TargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            string originalItemSpec = isResolvedItem
                ? projectChange.Before.GetProjectItemProperties(removedItem)?.GetStringProperty(ProjectItemMetadata.Name) ?? removedItem
                : removedItem;

            changesBuilder.Removed(targetFramework, ProviderTypeString, originalItemSpec);
        }

        public override IDependencyModel CreateRootDependencyNode() => s_groupModel;

        private static readonly InternPool<string> s_targetFrameworkInternPool = new(StringComparer.Ordinal);

        private bool TryCreatePackageDependencyModel(
            string projectFullPath,
            string itemSpec,
            bool isResolvedItem,
            IImmutableDictionary<string, string> properties,
            IProjectRuleSnapshot evaluationRuleSnapshot,
            Func<string, bool>? isEvaluatedItemSpec,
            TargetFramework targetFramework,
            DependenciesChangesBuilder changesBuilder,
            [NotNullWhen(returnValue: true)] out PackageDependencyModel? dependencyModel)
        {
            Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(targetFramework, nameof(targetFramework));

            string originalItemSpec;

            if (isEvaluatedItemSpec is not null)
            {
                // We have design-time build data
                string? name = properties.GetStringProperty(ProjectItemMetadata.Name);

                string? dependencyType = properties.GetStringProperty(ProjectItemMetadata.Type);

                if (dependencyType is not null)
                {
                    // LEGACY MODE
                    //
                    // In 16.7 (SDK 3.1.4xx) the format of ResolvedPackageReference items was changed in task PreprocessPackageDependenciesDesignTime.
                    //
                    // If we observe "Type" metadata then we are running with an older SDK and need to preserve some
                    // legacy behaviour to avoid breaking the dependencies node too much. Transitive dependencies will
                    // not be displayed, but we should be able to provide an equivalent experience for top-level items.

                    if (!StringComparers.PropertyLiteralValues.Equals(dependencyType, "Package"))
                    {
                        // Legacy behaviour included items of various types. We now only accept "Package".
                        dependencyModel = null;
                        return false;
                    }

                    // Legacy behaviour was to return packages for all targets, even though we have a build per-target.
                    // The package's target was prefixed to its ItemSpec (for example: ".NETFramework,Version=v4.8/MetadataExtractor/2.3.0").
                    // We would then filter out items for the wrong target here.
                    //
                    // From 16.7 we no longer return items from other target frameworks during DTB, and we remove the target prefix from ItemSpec.
                    //
                    // This code preserves filtering logic when processing legacy items.
                    int slashIndex = itemSpec.IndexOf('/');
                    if (slashIndex != -1)
                    {
                        string targetFrameworkName = s_targetFrameworkInternPool.Intern(itemSpec.Substring(0, slashIndex));

                        if (_targetFrameworkProvider.GetTargetFramework(targetFrameworkName)?.Equals(targetFramework) != true)
                        {
                            // Item is not for the correct target
                            dependencyModel = null;
                            return false;
                        }
                    }

                    // Name metadata is required in 16.7. Legacy behaviour uses ItemSpec as a fallback.
                    name ??= itemSpec;
                }
                else
                {
                    if (Strings.IsNullOrEmpty(name))
                    {
                        // This should not happen as Name is required in PreprocessPackageDependenciesDesignTime from 16.7
                        dependencyModel = null;
                        return false;
                    }
                }

                bool isTopLevel = isEvaluatedItemSpec(name);

                if (!isTopLevel)
                {
                    // We no longer accept non-top-level dependencies from DTB data. See note above about legacy mode support.
                    dependencyModel = null;
                    return false;
                }

                // PackageReference uses Name rather than OriginalItemSpec.
                originalItemSpec = name;
            }
            else
            {
                // We only have evaluation data

                System.Diagnostics.Debug.Assert(itemSpec.IndexOf('/') == -1);

                originalItemSpec = itemSpec;
            }

            IImmutableDictionary<string, string>? evaluationProperties = evaluationRuleSnapshot.GetProjectItemProperties(originalItemSpec);

            if (evaluationProperties is null)
            {
                // This package is present in build results, but not in evaluation.
                // We disallow packages which are not present in evaluation.
                dependencyModel = null;
                return false;
            }

            bool isImplicit = IsImplicit(projectFullPath, evaluationProperties);

            // When we only have evaluation data, mark the dependency as resolved if we currently have a corresponding resolved item
            bool isResolved = isResolvedItem || changesBuilder.HasResolvedItem(targetFramework, ProviderType, originalItemSpec);

            dependencyModel = new PackageDependencyModel(
                originalItemSpec,
                version: properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty,
                isResolved,
                isImplicit,
                isVisible: true,
                properties);
            return true;
        }
    }
}
