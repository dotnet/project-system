﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Imaging.VisualBasic;

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
        Requires.NotNullOrEmpty(key);

        return key switch
        {
            ProjectImageKey.ProjectRoot => KnownProjectImageMonikers.VBProjectNode,
            ProjectImageKey.SharedItemsImportFile or ProjectImageKey.SharedProjectRoot => KnownProjectImageMonikers.VBSharedProject,
            _ => null
        };
    }
}
