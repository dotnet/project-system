// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Changes explicit, resolved, top-level project dependencies to implicit if they are not present in the set of known project item specs.
    /// </summary>
    /// <remarks>
    /// Only applies to dependencies whose providers implement <see cref="IProjectDependenciesSubTreeProviderInternal"/>.
    /// </remarks>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class ImplicitTopLevelDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 130;

        public override void BeforeAddOrUpdate(
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string>? projectItemSpecs,
            AddDependencyContext context)
        {
            if (projectItemSpecs != null                                              // must have data
                && dependency.TopLevel                                                // top-level
                && !dependency.Implicit                                               // explicit
                && dependency.Resolved                                                // resolved
                && dependency.Flags.Contains(DependencyTreeFlags.GenericDependency)   // generic dependency
                && !dependency.Flags.Contains(DependencyTreeFlags.SharedProjectFlags) // except for shared projects
                && !projectItemSpecs.Contains(dependency.OriginalItemSpec)            // is not a known item spec
                && subTreeProviderByProviderType.TryGetValue(dependency.ProviderType, out IProjectDependenciesSubTreeProvider provider)
                && provider is IProjectDependenciesSubTreeProviderInternal internalProvider)
            {
                // Obtain custom implicit icon
                ImageMoniker implicitIcon = internalProvider.ImplicitIcon;

                // Obtain a pooled icon set with this implicit icon
                DependencyIconSet implicitIconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(
                    implicitIcon,
                    implicitIcon,
                    dependency.IconSet.UnresolvedIcon,
                    dependency.IconSet.UnresolvedExpandedIcon);

                context.Accept(
                    dependency.SetProperties(
                        iconSet: implicitIconSet,
                        isImplicit: true));
                return;
            }

            context.Accept(dependency);
        }
    }
}
