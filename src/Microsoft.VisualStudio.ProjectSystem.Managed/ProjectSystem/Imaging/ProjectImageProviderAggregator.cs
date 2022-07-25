// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Imaging
{
    /// <summary>
    ///     Aggregates <see cref="IProjectImageProvider"/> instances into a single importable
    ///     <see cref="IProjectImageProvider"/>.
    /// </summary>
    [Export]
    [AppliesTo(ProjectCapability.DotNet)]
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal class ProjectImageProviderAggregator : IProjectImageProvider
    {
        [ImportingConstructor]
        public ProjectImageProviderAggregator(UnconfiguredProject project)
        {
            ImageProviders = new OrderPrecedenceImportCollection<IProjectImageProvider>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectImageProvider> ImageProviders { get; }

        public ProjectImageMoniker? GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            foreach (Lazy<IProjectImageProvider> provider in ImageProviders)
            {
                ProjectImageMoniker? image = provider.Value.GetProjectImage(key);

                if (image is not null)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
