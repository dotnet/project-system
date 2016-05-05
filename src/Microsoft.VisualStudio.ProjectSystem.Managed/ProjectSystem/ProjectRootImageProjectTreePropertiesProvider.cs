// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private readonly IProjectImageProvider _imageProvider;

        [ImportingConstructor]
        public ProjectRootImageProjectTreePropertiesProvider([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider imageProvider)
        {
            Requires.NotNull(imageProvider, nameof(imageProvider));

            _imageProvider = imageProvider;
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
            {
                ProjectImageMoniker icon = _imageProvider.GetProjectImage(ProjectImageKey.ProjectRoot);
                if (icon == null)
                    return;

                propertyValues.Icon = icon;
            }
        }
    }
}

