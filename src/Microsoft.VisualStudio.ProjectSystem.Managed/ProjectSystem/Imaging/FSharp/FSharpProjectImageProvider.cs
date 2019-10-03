// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
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
