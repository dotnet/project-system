// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class TargetDependencyViewModel : DependencyViewModel
    {
        public TargetDependencyViewModel(ITargetedDependenciesSnapshot snapshot)
        {
            Caption = snapshot.TargetFramework.FriendlyName;
            Icon = snapshot.HasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
            ExpandedIcon = Icon;
            Flags = ProjectTreeFlags.Empty.Add(DependencyTreeFlags.TargetNodeFlags.ToString());
        }
    }
}
