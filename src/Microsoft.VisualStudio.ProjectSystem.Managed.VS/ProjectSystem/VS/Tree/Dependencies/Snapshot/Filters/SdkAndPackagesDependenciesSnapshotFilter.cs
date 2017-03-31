// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Sdk nodes are actually packages and their hierarchy of dependencies is resolved from
    /// NuGet's assets json file. However Sdk them selves are brought by DesignTime build for rules
    /// SdkReference. This filter matches Sdk to their corresponding NuGet package and sets  
    /// of top level sdk dependencies from the package. Packages are in visible to avoid visual
    /// duplication and confusion.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class SdkAndPackagesDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 110;

        public override IDependency BeforeAdd(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency, 
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder)
        {
            IDependency resultDependency = dependency;
            if (!dependency.TopLevel || !dependency.Resolved)
            {
                return resultDependency;
            }

            if (dependency.Flags.Contains(DependencyTreeFlags.SdkSubTreeNodeFlags))
            {
                // find package with the same name
                var packageModelId = dependency.Name;
                var packageId = Dependency.GetID(targetFramework, PackageRuleHandler.ProviderTypeString, packageModelId);

                if (worldBuilder.TryGetValue(packageId, out IDependency package) && package.Resolved)
                {
                    resultDependency = dependency.SetProperties(dependencyIDs:package.DependencyIDs);
                }
            }
            else if (dependency.Flags.Contains(DependencyTreeFlags.PackageNodeFlags))
            {
                // find sdk with the same name
                var sdkModelId = dependency.Name;
                var sdkId = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, sdkModelId);

                if (worldBuilder.TryGetValue(sdkId, out IDependency sdk) && sdk.Resolved)
                {
                    sdk = sdk.SetProperties(dependencyIDs:dependency.DependencyIDs);
                    worldBuilder[sdk.Id] = sdk;
                }
            }

            return resultDependency;
        }

        public override void BeforeRemove(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency, 
            ImmutableDictionary<string, IDependency>.Builder worldBuilder,
            ImmutableHashSet<IDependency>.Builder topLevelBuilder)
        {
            if (!dependency.TopLevel || !dependency.Resolved)
            {
                return;
            }

            if (dependency.Flags.Contains(DependencyTreeFlags.PackageNodeFlags))
            {
                // find sdk with the same name and clean dependencyIDs
                var sdkModelId = dependency.Name;
                var sdkId = Dependency.GetID(targetFramework, SdkRuleHandler.ProviderTypeString, sdkModelId);

                if (worldBuilder.TryGetValue(sdkId, out IDependency sdk))
                {
                    // clean up sdk when corresponding package is removing
                    sdk = sdk.SetProperties(dependencyIDs:ImmutableList<string>.Empty);
                    worldBuilder[sdk.Id] = sdk;
                }
            }
        }
    }
}
