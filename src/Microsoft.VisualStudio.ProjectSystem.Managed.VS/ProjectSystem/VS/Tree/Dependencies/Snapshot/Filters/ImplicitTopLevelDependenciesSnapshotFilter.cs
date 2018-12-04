// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters
{
    /// <summary>
    /// Changes resolved top level project dependencies to unresolved if:
    ///     - dependent project does not have targets supporting given target framework in current project
    ///     - dependent project has any unresolved dependencies in a snapshot for given target framework
    /// This helps to bubble up error status (yellow icon) for project dependencies.
    /// </summary>
    [Export(typeof(IDependenciesSnapshotFilter))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal sealed class ImplicitTopLevelDependenciesSnapshotFilter : DependenciesSnapshotFilterBase
    {
        public const int Order = 130;

        public override void BeforeAddOrUpdate(
            string projectPath,
            ITargetFramework targetFramework,
            IDependency dependency,
            IReadOnlyDictionary<string, IProjectDependenciesSubTreeProvider> subTreeProviderByProviderType,
            IImmutableSet<string> projectItemSpecs,
            IAddDependencyContext context)
        {
            if (!dependency.TopLevel
                || dependency.Implicit
                || !dependency.Resolved
                || !dependency.Flags.Contains(DependencyTreeFlags.GenericDependencyFlags))
            {
                context.Accept(dependency);
                return;
            }

            if (projectItemSpecs == null)
            {
                // No data, so don't update
                context.Accept(dependency);
                return;
            }

            if (!projectItemSpecs.Contains(dependency.OriginalItemSpec))
            {
                // It is an implicit dependency
                if (subTreeProviderByProviderType.TryGetValue(dependency.ProviderType, out IProjectDependenciesSubTreeProvider provider) && 
                    provider is IProjectDependenciesSubTreeProviderInternal internalProvider)
                {
                    ImageMoniker implicitIcon = internalProvider.GetImplicitIcon();

                    DependencyIconSet implicitIconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(
                        implicitIcon,
                        implicitIcon,
                        dependency.IconSet.UnresolvedIcon,
                        dependency.IconSet.UnresolvedExpandedIcon);

                    context.Accept(dependency.SetProperties(
                        iconSet: implicitIconSet,
                        isImplicit: true));
                    return;
                }
            }

            context.Accept(dependency);
        }
    }
}
