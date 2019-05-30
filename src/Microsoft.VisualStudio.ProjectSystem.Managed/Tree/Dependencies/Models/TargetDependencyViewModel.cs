// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal sealed class TargetDependencyViewModel : IDependencyViewModel
    {
        private readonly bool _hasUnresolvedDependency;

        public TargetDependencyViewModel(ITargetedDependenciesSnapshot snapshot)
        {
            Caption = snapshot.TargetFramework.FriendlyName;
            Flags = DependencyTreeFlags.TargetNodeFlags.Add($"$TFM:{snapshot.TargetFramework.FullName}");
            _hasUnresolvedDependency = snapshot.HasUnresolvedDependency;
        }

        public string Caption { get; }
        public string FilePath => null;
        public string SchemaName => null;
        public string SchemaItemType => null;
        public int Priority => Dependency.FrameworkAssemblyNodePriority;
        public ImageMoniker Icon => _hasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
        public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
        public IImmutableDictionary<string, string> Properties => null;
        public ProjectTreeFlags Flags { get; }
        public IDependency OriginalModel => null;
    }
}
