// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Sdk nodes are actually packages and their hierarchy of dependencies is resolved from
    /// NuGet's assets json file. However Sdk themselves are brought by DesignTime build for rules
    /// SdkReference. This filter matches Sdk to their corresponding NuGet package and sets  
    /// of top level sdk dependencies from the package. Packages are invisible to avoid visual
    /// duplication and confusion.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class SdkAndPackagesDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 110;

        public override void BeforeAddOrUpdate(
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            if (dependency.Flags.Contains(DependencyTreeFlags.SdkDependency))
            {
                // This is an SDK dependency.
                //
                // Try to find a resolved package dependency with the same name.

                string packageId = Dependency.GetID(PackageRuleHandler.ProviderTypeString, modelId: dependency.Name);

                if (context.TryGetDependency(packageId, out IDependency package) && package.Resolved)
                {
                    // Set to resolved, and copy dependencies.

                    context.Accept(dependency.ToResolved(
                        schemaName: ResolvedSdkReference.SchemaName));
                    return;
                }
            }
            else if (dependency.Flags.Contains(DependencyTreeFlags.PackageDependency) && dependency.Resolved)
            {
                // This is a resolved package dependency.
                //
                // Try to find an SDK dependency with the same name.

                string sdkId = Dependency.GetID(SdkRuleHandler.ProviderTypeString, modelId: dependency.Name);

                if (context.TryGetDependency(sdkId, out IDependency sdk))
                {
                    // We have an SDK dependency for this package. Such dependencies, when implicit, are created
                    // as unresolved by SdkRuleHandler, and are only marked resolved here once we have resolved the
                    // corresponding package.
                    //
                    // Set to resolved, and copy dependencies.

                    context.AddOrUpdate(sdk.ToResolved(
                        schemaName: ResolvedSdkReference.SchemaName));
                }
            }

            context.Accept(dependency);
        }

        public override void BeforeRemove(
            IDependency dependency,
            RemoveDependencyContext context)
        {
            if (dependency.Resolved &&
                dependency.Flags.Contains(DependencyTreeFlags.PackageDependency))
            {
                // This is a package dependency.
                //
                // Try to find an SDK dependency with the same name.

                string sdkId = Dependency.GetID(SdkRuleHandler.ProviderTypeString, modelId: dependency.Name);

                if (context.TryGetDependency(sdkId, out IDependency sdk))
                {
                    // We are removing the package dependency related to this SDK dependency
                    // and must undo the changes made above in BeforeAdd.
                    //
                    // Set to unresolved, and clear dependencies.

                    context.AddOrUpdate(sdk.ToUnresolved(
                        schemaName: SdkReference.SchemaName));
                }
            }

            context.Accept();
        }
    }
}
