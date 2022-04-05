// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Imaging.FSharp
{
    /// <summary>
    ///     Provides F# project images.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpProjectImageProvider : IProjectImageProvider
    {
        [ImportingConstructor]
        public FSharpProjectImageProvider()
        {
        }

        public ProjectImageMoniker? GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            return key == ProjectImageKey.ProjectRoot ?
                KnownMonikers.FSProjectNode.ToProjectSystemType() :
                null;
        }
    }
}
