// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
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
        public OrderPrecedenceImportCollection<IProjectImageProvider> ImageProviders
        {
            get;
        }

        public ProjectImageMoniker? GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            foreach (Lazy<IProjectImageProvider> provider in ImageProviders)
            {
                ProjectImageMoniker? image = provider.Value.GetProjectImage(key);

                if (image != null)
                {
                    return image;
                }
            }

            return null;
        }
    }
}
