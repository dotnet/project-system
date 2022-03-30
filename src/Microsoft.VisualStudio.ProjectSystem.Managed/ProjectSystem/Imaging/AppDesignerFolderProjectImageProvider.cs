// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Imaging
{
    /// <summary>
    ///     Provides the AppDesigner folder image.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.AppDesigner)]
    internal class AppDesignerFolderProjectImageProvider : IProjectImageProvider
    {
        private static readonly ProjectImageMoniker s_iconClosed = KnownMonikers.PropertiesFolderClosed.ToProjectSystemType();
        private static readonly ProjectImageMoniker s_iconOpened = KnownMonikers.PropertiesFolderOpen.ToProjectSystemType();

        [ImportingConstructor]
        public AppDesignerFolderProjectImageProvider()
        {
        }

        public ProjectImageMoniker? GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            return key switch
            {
                ProjectImageKey.AppDesignerFolder => s_iconClosed,
                ProjectImageKey.ExpandedAppDesignerFolder => s_iconOpened,
                _ => null
            };
        }
    }
}
