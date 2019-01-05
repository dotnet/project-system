// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract,
            typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed partial class PackageRuleHandler : DependenciesRuleHandlerBase
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
            CrossTargetDependenciesChangesBuilder changesBuilder)
        {
            var caseInsensitiveUnresolvedChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (changesByRuleName.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges))
            {
                caseInsensitiveUnresolvedChanges.AddRange(unresolvedChanges.After.Items.Keys);

                if (unresolvedChanges.Difference.AnyChanges)
                {
                    HandleChangesForRule(
                        unresolvedChanges,
                        changesBuilder,
                        targetFramework,
                        resolved: false);
                }
            }

            if (changesByRuleName.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges)
                && resolvedChanges.Difference.AnyChanges)
            {
                HandleChangesForRule(
                    resolvedChanges,
                    changesBuilder,
                    targetFramework,
                    resolved: true,
                    unresolvedChanges: caseInsensitiveUnresolvedChanges);
            }
        }

        private void HandleChangesForRule(
            IProjectChangeDescription projectChange,
            CrossTargetDependenciesChangesBuilder changesBuilder,
            ITargetFramework targetFramework,
            bool resolved,
            HashSet<string> unresolvedChanges = null)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));

            foreach (string removedItem in projectChange.Difference.RemovedItems)
            {
                if (PackageDependencyMetadata.TryGetMetadata(
                    removedItem,
                    resolved,
                    properties: projectChange.Before.GetProjectItemProperties(removedItem),
                    unresolvedChanges,
                    targetFramework,
                    TargetFrameworkProvider,
                    out PackageDependencyMetadata metadata))
                {
                    changesBuilder.Removed(targetFramework, ProviderTypeString, metadata.OriginalItemSpec);
                }
            }

            foreach (string changedItem in projectChange.Difference.ChangedItems)
            {
                if (PackageDependencyMetadata.TryGetMetadata(
                    changedItem,
                    resolved,
                    properties: projectChange.After.GetProjectItemProperties(changedItem),
                    unresolvedChanges,
                    targetFramework,
                    TargetFrameworkProvider,
                    out PackageDependencyMetadata metadata))
                {
                    changesBuilder.Removed(targetFramework, ProviderTypeString, metadata.OriginalItemSpec);
                    changesBuilder.Added(targetFramework, metadata.CreateDependencyModel());
                }
            }

            foreach (string addedItem in projectChange.Difference.AddedItems)
            {
                if (PackageDependencyMetadata.TryGetMetadata(
                    addedItem,
                    resolved,
                    properties: projectChange.After.GetProjectItemProperties(addedItem),
                    unresolvedChanges,
                    targetFramework,
                    TargetFrameworkProvider,
                    out PackageDependencyMetadata metadata))
                {
                    changesBuilder.Added(targetFramework, metadata.CreateDependencyModel());
                }
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;
    }
}
