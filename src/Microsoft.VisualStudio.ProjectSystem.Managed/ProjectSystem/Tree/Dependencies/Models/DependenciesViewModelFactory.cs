// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
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

        public IDependencyViewModel CreateTargetViewModel(TargetFramework targetFramework, DiagnosticLevel maximumDiagnosticLevel)
        {
            return new TargetDependencyViewModel(targetFramework, maximumDiagnosticLevel);
        }

        public (IDependencyViewModel? GroupNodeViewModel, ProjectTreeFlags? GroupNodeFlag) CreateGroupNodeViewModel(string providerType, DiagnosticLevel maximumDiagnosticLevel)
        {
            IProjectDependenciesSubTreeProvider? provider = GetProvider();

            IDependencyModel? dependencyModel = provider?.CreateRootDependencyNode();

            IDependencyViewModel? groupNodeViewModel = dependencyModel?.ToViewModel(maximumDiagnosticLevel);

            ProjectTreeFlags? groupNodeFlag = provider is IProjectDependenciesSubTreeProvider2 provider2
                ? provider2.GroupNodeFlag
                : null;

            return (groupNodeViewModel, groupNodeFlag);

            IProjectDependenciesSubTreeProvider? GetProvider()
            {
                return SubTreeProviders
                    .FirstOrDefault((x, t) => StringComparers.DependencyProviderTypes.Equals(x.Value.ProviderType, t), providerType)
                    ?.Value;
            }
        }

        public ImageMoniker GetDependenciesRootIcon(DiagnosticLevel maximumDiagnosticLevel)
        {
            return maximumDiagnosticLevel switch
            {
                DiagnosticLevel.None => KnownMonikers.ReferenceGroup,
                _ => KnownMonikers.ReferenceGroupWarning
            };
        }
    }
}
