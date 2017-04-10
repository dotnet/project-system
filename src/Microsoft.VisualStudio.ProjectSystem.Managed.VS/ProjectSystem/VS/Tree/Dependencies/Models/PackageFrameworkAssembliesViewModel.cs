// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal class PackageFrameworkAssembliesViewModel : DependencyViewModel
    {
        public static ImageMoniker RegularIcon = KnownMonikers.Library;

        public PackageFrameworkAssembliesViewModel()
        {
            Caption = VSResources.FrameworkAssembliesNodeName;
            Icon = RegularIcon;
            ExpandedIcon = Icon;
            Priority = Dependency.FrameworkAssemblyNodePriority;
            Flags = DependencyTreeFlags.FrameworkAssembliesNodeFlags;
        }
    }
}
