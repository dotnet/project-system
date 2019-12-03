// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal sealed class TargetDependencyViewModel : IDependencyViewModel
    {
        private readonly bool _hasUnresolvedDependency;

        public TargetDependencyViewModel(TargetedDependenciesSnapshot snapshot)
        {
            Caption = snapshot.TargetFramework.FriendlyName;
            Flags = DependencyTreeFlags.TargetNode.Add($"$TFM:{snapshot.TargetFramework.FullName}");
            _hasUnresolvedDependency = snapshot.HasReachableVisibleUnresolvedDependency;
        }

        public string Caption { get; }
        public string? FilePath => null;
        public string? SchemaName => null;
        public string? SchemaItemType => null;
        public int Priority => GraphNodePriority.FrameworkAssembly;
        public ImageMoniker Icon => _hasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
        public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
        public ProjectTreeFlags Flags { get; }
        public IDependency? OriginalModel => null;
    }
}
