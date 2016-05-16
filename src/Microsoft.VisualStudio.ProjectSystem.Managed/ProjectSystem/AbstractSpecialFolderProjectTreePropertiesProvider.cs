// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides the base class for <see cref="IProjectTreePropertiesProvider"/> objects that handle special items, such as the AppDesigner folder.
    /// </summary>
    internal abstract class AbstractSpecialFolderProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        private readonly IProjectImageProvider _imageProvider;

        protected AbstractSpecialFolderProjectTreePropertiesProvider(IProjectImageProvider imageProvider)
        {
            Requires.NotNull(imageProvider, nameof(imageProvider));

            _imageProvider = imageProvider;
        }

        /// <summary>
        ///     Gets the image key that represents the image that will be applied to the special folder.
        /// </summary>
        public abstract string FolderImageKey
        {
            get;
        }

        /// <summary>
        ///     Gets the default flags that will be applied to the special folder.
        /// </summary>
        public abstract ProjectTreeFlags FolderFlags
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating whether the special folder is supported in this project.
        /// </summary>
        public abstract bool IsSupported
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating whether the contents of the special folder are only visibile in Show All Files.
        /// </summary>
        public abstract bool ContentsVisibleOnlyInShowAllFiles
        {
            get;
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            Requires.NotNull(propertyContext, nameof(propertyContext));
            Requires.NotNull(propertyValues, nameof(propertyValues));

            if (IsSupported && IsCandidateSpecialFolder(propertyContext, propertyValues.Flags))
            {
                propertyValues.Flags = propertyValues.Flags.Union(FolderFlags);

                // Avoid overwriting icon if the image provider didn't provide one
                ProjectImageMoniker icon = _imageProvider.GetProjectImage(FolderImageKey);
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
        protected abstract bool IsCandidateSpecialFolder(IProjectTreeCustomizablePropertyContext propertyContext, ProjectTreeFlags currentFlags);
    }
}
