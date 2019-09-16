// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Imaging.CSharp
{
    /// <summary>
    ///     Provides C# project images.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpProjectImageProvider : IProjectImageProvider
    {
        [ImportingConstructor]
        public CSharpProjectImageProvider()
        {
        }

        public ProjectImageMoniker? GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            switch (key)
            {
                case ProjectImageKey.ProjectRoot:
                    return KnownMonikers.CSProjectNode.ToProjectSystemType();

                case ProjectImageKey.SharedItemsImportFile:
                case ProjectImageKey.SharedProjectRoot:
                    return KnownMonikers.CSSharedProject.ToProjectSystemType();

                default:
                    return null;
            }
        }
    }
}
