// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    internal sealed class PackageFrameworkAssembliesViewModel : IDependencyViewModel
    {
        public static PackageFrameworkAssembliesViewModel Instance { get; } = new PackageFrameworkAssembliesViewModel();

        public static readonly ImageMoniker RegularIcon = KnownMonikers.Library;

        public string Caption => Resources.FrameworkAssembliesNodeName;
        public string? FilePath => null;
        public string? SchemaName => null;
        public string? SchemaItemType => null;
        public int Priority => GraphNodePriority.FrameworkAssembly;
        public ImageMoniker Icon => RegularIcon;
        public ImageMoniker ExpandedIcon => RegularIcon;
        public ProjectTreeFlags Flags => DependencyTreeFlags.FrameworkAssembliesNode;
        public IDependency? OriginalModel => null;
    }
}
