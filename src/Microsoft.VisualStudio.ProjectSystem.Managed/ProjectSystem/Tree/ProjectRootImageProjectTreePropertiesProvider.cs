// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;
using ManagedPriorityOrder = Microsoft.VisualStudio.ProjectSystem.Order;

namespace Microsoft.VisualStudio.ProjectSystem.Tree
{
    /// <summary>
    ///     Modifies the Solution Explorer tree image for the project root.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    [Order(ManagedPriorityOrder.Default)]
    internal class ProjectRootImageProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        private readonly IProjectCapabilitiesService _capabilities;
        private readonly IProjectImageProvider _imageProvider;

        [ImportingConstructor]
        public ProjectRootImageProjectTreePropertiesProvider(IProjectCapabilitiesService capabilities, [Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider)
        {
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
            ProjectImageMoniker? icon = _imageProvider.GetProjectImage(imageKey);

            if (icon is not null)
            {
                propertyValues.Icon = icon;
            }
        }
    }
}

