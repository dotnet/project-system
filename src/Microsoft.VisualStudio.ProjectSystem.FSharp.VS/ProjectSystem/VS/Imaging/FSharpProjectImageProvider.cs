// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Imaging
{
    /// <summary>
    ///     Provides C# project images.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.FSharp)]
    [Order(2)]
    internal class FSharpProjectImageProvider : IProjectImageProvider
    {
        [ImportingConstructor]
        public FSharpProjectImageProvider()
        {
        }

        public ProjectImageMoniker GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            switch (key)
            {
                case ProjectImageKey.ProjectRoot:
                    return KnownMonikers.FSProjectNode.ToProjectSystemType();

                case ProjectImageKey.AppDesignerFolder:
                    return KnownMonikers.Property.ToProjectSystemType();

                default:
                    return null;
            }
        }
    }
}
