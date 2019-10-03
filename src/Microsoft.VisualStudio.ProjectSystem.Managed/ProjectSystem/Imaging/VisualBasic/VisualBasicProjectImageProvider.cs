// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Imaging.VisualBasic
{
    /// <summary>
    ///     Provides Visual Basic project images.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicProjectImageProvider : IProjectImageProvider
    {
        [ImportingConstructor]
        public VisualBasicProjectImageProvider()
        {
        }

        public ProjectImageMoniker? GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            switch (key)
            {
                case ProjectImageKey.ProjectRoot:
                    return KnownMonikers.VBProjectNode.ToProjectSystemType();

                case ProjectImageKey.SharedItemsImportFile:
                case ProjectImageKey.SharedProjectRoot:
                    return KnownMonikers.VBSharedProject.ToProjectSystemType();

                default:
                    return null;
            }
        }
    }
}
