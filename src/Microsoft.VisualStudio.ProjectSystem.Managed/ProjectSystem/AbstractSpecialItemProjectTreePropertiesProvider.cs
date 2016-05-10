// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides the base class for <see cref="IProjectTreePropertiesProvider"/> objects that handle special items, such as the AppDesigner folder.
    /// </summary>
    internal abstract class AbstractSpecialItemProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        private readonly IProjectImageProvider _imageProvider;

        protected AbstractSpecialItemProjectTreePropertiesProvider(IProjectImageProvider imageProvider)
        {
            Requires.NotNull(imageProvider, nameof(imageProvider));

            _imageProvider = imageProvider;
        }

        /// <summary>
        ///     Gets the image key that represents the image that will be applied to the candidate special item.
        /// </summary>
        public abstract string ImageKey
        {
            get;
        }

        /// <summary>
        ///     Gets the default flags that will be applied to the candidate special item.
        /// </summary>
        public abstract ProjectTreeFlags Flags
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating whether the special item is supported in this project.
        /// </summary>
        public abstract bool IsSupported
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating whether the special item is expandable by default.
        /// </summary>
        public abstract bool IsExpandableByDefault
        {
            get;
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            Requires.NotNull(propertyContext, nameof(propertyContext));
            Requires.NotNull(propertyValues, nameof(propertyValues));

            if (IsSupported && IsCandidateSpecialItem(propertyContext, propertyValues.Flags))
            {
                propertyValues.Flags = propertyValues.Flags.Union(Flags);

                // Avoid overwriting icon if the image provider didn't provide one
                ProjectImageMoniker icon = _imageProvider.GetProjectImage(ImageKey);
                if (icon != null)
                {
                    propertyValues.Icon = icon;
                    propertyValues.ExpandedIcon = icon;
                }
            }
        }

        /// <summary>
        ///     Returns a value indicating whether the specified property context represents the candidate special item.
        /// </summary>
        protected abstract bool IsCandidateSpecialItem(IProjectTreeCustomizablePropertyContext propertyContext, ProjectTreeFlags currentFlags);
    }
}
