// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            Requires.NotNullOrEmpty(key);

            return key switch
            {
                ProjectImageKey.ProjectRoot => KnownProjectImageMonikers.CSProjectNode,
                ProjectImageKey.SharedItemsImportFile or ProjectImageKey.SharedProjectRoot => KnownProjectImageMonikers.CSSharedProject,
                _ => null
            };
        }
    }
}
