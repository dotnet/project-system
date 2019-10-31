// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    [Export(DependencyRulesSubscriber.DependencyRulesSubscriberContract, typeof(IDependenciesRuleHandler))]
    [Export(typeof(IProjectDependenciesSubTreeProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal sealed partial class PackageRuleHandler : DependenciesRuleHandlerBase
    {
        public const string ProviderTypeString = "NuGetDependency";

        private static readonly SubTreeRootDependencyModel s_rootModel = new SubTreeRootDependencyModel(
            ProviderTypeString,
            Resources.PackagesNodeName,
            new DependencyIconSet(
                icon: ManagedImageMonikers.NuGetGrey,
                expandedIcon: ManagedImageMonikers.NuGetGrey,
                unresolvedIcon: ManagedImageMonikers.NuGetGreyWarning,
                unresolvedExpandedIcon: ManagedImageMonikers.NuGetGreyWarning));

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
            if (PackageDependencyMetadata.TryGetMetadata(
                addedItem,
                resolved,
                properties: projectChange.After.GetProjectItemProperties(addedItem)!,
                isEvaluatedItemSpec,
                targetFramework,
                _targetFrameworkProvider,
                out PackageDependencyMetadata metadata))
            {
                changesBuilder.Added(metadata.CreateDependencyModel());
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
            if (PackageDependencyMetadata.TryGetMetadata(
                changedItem,
                resolved,
                properties: projectChange.After.GetProjectItemProperties(changedItem)!,
                isEvaluatedItemSpec,
                targetFramework,
                _targetFrameworkProvider,
                out PackageDependencyMetadata metadata))
            {
                changesBuilder.Removed(ProviderTypeString, metadata.OriginalItemSpec);
                changesBuilder.Added(metadata.CreateDependencyModel());
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
            if (PackageDependencyMetadata.TryGetMetadata(
                removedItem,
                resolved,
                properties: projectChange.Before.GetProjectItemProperties(removedItem)!,
                isEvaluatedItemSpec,
                targetFramework,
                _targetFrameworkProvider,
                out PackageDependencyMetadata metadata))
            {
                changesBuilder.Removed(ProviderTypeString, metadata.OriginalItemSpec);
            }
        }

        public override IDependencyModel CreateRootDependencyNode() => s_rootModel;
    }
}
