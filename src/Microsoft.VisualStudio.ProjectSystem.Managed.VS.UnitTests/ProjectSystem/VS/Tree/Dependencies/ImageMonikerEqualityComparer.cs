// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal sealed class ImageMonikerEqualityComparer : IEqualityComparer<ImageMoniker>
    {
        public static ImageMonikerEqualityComparer Instance { get; } = new ImageMonikerEqualityComparer();

        public bool Equals(ImageMoniker x, ImageMoniker y) => x.Id == y.Id && x.Guid == y.Guid;

        public int GetHashCode(ImageMoniker obj) => (obj.Id * 397) ^ obj.Guid.GetHashCode();
    }
}
