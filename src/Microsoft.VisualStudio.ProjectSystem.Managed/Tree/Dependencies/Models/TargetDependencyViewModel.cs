// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal sealed class TargetDependencyViewModel : IDependencyViewModel
    {
        private readonly bool _hasUnresolvedDependency;

        public TargetDependencyViewModel(ITargetFramework targetFramework, bool hasReachableVisibleUnresolvedDependency)
        {
            Caption = targetFramework.FriendlyName;
            Flags = DependencyTreeFlags.DependencyConfigurationGroup.Add($"$TFM:{targetFramework.FullName}");
            _hasUnresolvedDependency = hasReachableVisibleUnresolvedDependency;
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
