// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal static class IDependencyViewModelExtensions
    {
        public static IEnumerable<ImageMoniker> GetIcons(this IDependencyViewModel self)
        {
            yield return self.Icon;
            yield return self.ExpandedIcon;
        }
    }
}
