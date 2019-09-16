// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

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

        public IDependencyViewModel CreateTargetViewModel(TargetedDependenciesSnapshot snapshot)
        {
            return new TargetDependencyViewModel(snapshot);
        }

        public IDependencyViewModel? CreateRootViewModel(string providerType, bool hasUnresolvedDependency)
        {
            IProjectDependenciesSubTreeProvider? provider = GetProvider();

            IDependencyModel? dependencyModel = provider?.CreateRootDependencyNode();

            return dependencyModel?.ToViewModel(hasUnresolvedDependency);

            IProjectDependenciesSubTreeProvider? GetProvider()
            {
                return SubTreeProviders
                    .FirstOrDefault((x, t) => StringComparers.DependencyProviderTypes.Equals(x.Value.ProviderType, t), providerType)
                    ?.Value;
            }
        }

        public ImageMoniker GetDependenciesRootIcon(bool hasUnresolvedDependencies)
        {
            return hasUnresolvedDependencies
                ? ManagedImageMonikers.ReferenceGroupWarning
                : ManagedImageMonikers.ReferenceGroup;
        }
    }
}
