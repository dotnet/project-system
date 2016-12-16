// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Modifies the Solution Explorer tree image for the project root.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ProjectRootImageProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        private readonly IProjectCapabilitiesService _capabilities;
        private readonly IProjectImageProvider _imageProvider;

        [ImportingConstructor]
        public ProjectRootImageProjectTreePropertiesProvider(IProjectCapabilitiesService capabilities, [Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider)
        {
            Requires.NotNull(capabilities, nameof(capabilities));
            Requires.NotNull(imageProvider, nameof(imageProvider));

            _capabilities = capabilities;
            _imageProvider = imageProvider;
        }

        private bool IsSharedProject
        {
            get { return _capabilities.Contains(ProjectCapabilities.SharedAssetsProject); }
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            Requires.NotNull(propertyContext, nameof(propertyContext));
            Requires.NotNull(propertyValues, nameof(propertyValues));

            if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
            {
                SetImage(propertyValues, IsSharedProject ? ProjectImageKey.SharedProjectRoot : ProjectImageKey.ProjectRoot);
            }
            else if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.SharedItemsImportFile))
            {
                SetImage(propertyValues, ProjectImageKey.SharedItemsImportFile);
            }
        }

        private void SetImage(IProjectTreeCustomizablePropertyValues propertyValues, string imageKey)
        {
            ProjectImageMoniker icon = _imageProvider.GetProjectImage(imageKey);
            if (icon == null)
                return;

            propertyValues.Icon = icon;
        }
    }
}

