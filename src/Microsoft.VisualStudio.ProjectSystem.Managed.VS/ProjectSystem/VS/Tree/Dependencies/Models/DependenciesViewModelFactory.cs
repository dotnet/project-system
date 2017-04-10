// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    [Export(typeof(IDependenciesViewModelFactory))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependenciesViewModelFactory : IDependenciesViewModelFactory
    {
        [ImportingConstructor]
        public DependenciesViewModelFactory(UnconfiguredProject unconfiguredProject)
        {
            Project = unconfiguredProject;
            SubTreeProviders = new OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider>(
                        ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesLast,
                        projectCapabilityCheckProvider: unconfiguredProject);
        }

        private UnconfiguredProject Project { get; }

        [ImportMany]
        protected OrderPrecedenceImportCollection<IProjectDependenciesSubTreeProvider> SubTreeProviders { get; set; }

        public IDependencyViewModel CreateTargetViewModel(ITargetedDependenciesSnapshot snapshot)
        {
            return new TargetDependencyViewModel(snapshot);
        }

        public IDependencyViewModel CreateRootViewModel(string providerType, bool hasUnresolvedDependency)
        {
            var provider = GetProvider(providerType);
            return provider.CreateRootDependencyNode().ToViewModel(hasUnresolvedDependency);
        }

        public ImageMoniker GetDependenciesRootIcon(bool hasUnresolvedDependencies)
        {
            return hasUnresolvedDependencies 
                ? ManagedImageMonikers.ReferenceGroupWarning
                : ManagedImageMonikers.ReferenceGroup;
        }

        private IProjectDependenciesSubTreeProvider GetProvider(string providerType)
        {
            return SubTreeProviders.FirstOrDefault(x => x.Value.ProviderType.Equals(providerType)).Value;
        }
    }
}
