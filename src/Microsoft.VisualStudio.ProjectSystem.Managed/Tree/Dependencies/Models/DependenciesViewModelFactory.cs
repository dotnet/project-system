// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    [Export(typeof(IDependenciesViewModelFactory))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesViewModelFactory : IDependenciesViewModelFactory
    {
        [ImportingConstructor]
        public DependenciesViewModelFactory(UnconfiguredProject project)
        {
            SubTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> SubTreeProviders { get; }

        public IDependencyViewModel CreateTargetViewModel(ITargetFramework targetFramework, bool hasReachableVisibleUnresolvedDependency)
        {
            return new TargetDependencyViewModel(targetFramework, hasReachableVisibleUnresolvedDependency);
        }

        public IDependencyViewModel? CreateGroupNodeViewModel(string providerType, bool hasReachableVisibleUnresolvedDependency)
        {
            IProjectDependenciesSubTreeProvider? provider = GetProvider();

            IDependencyModel? dependencyModel = provider?.CreateRootDependencyNode();

            return dependencyModel?.ToViewModel(hasReachableVisibleUnresolvedDependency);

            IProjectDependenciesSubTreeProvider? GetProvider()
            {
                return SubTreeProviders
                    .FirstOrDefault((x, t) => StringComparers.DependencyProviderTypes.Equals(x.Value.ProviderType, t), providerType)
                    ?.Value;
            }
        }

        public ImageMoniker GetDependenciesRootIcon(bool hasReachableVisibleUnresolvedDependency)
        {
            return hasReachableVisibleUnresolvedDependency
                ? ManagedImageMonikers.ReferenceGroupWarning
                : ManagedImageMonikers.ReferenceGroup;
        }
    }
}
