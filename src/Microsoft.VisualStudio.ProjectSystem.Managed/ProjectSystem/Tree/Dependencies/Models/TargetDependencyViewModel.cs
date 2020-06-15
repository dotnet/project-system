// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal sealed class TargetDependencyViewModel : IDependencyViewModel
    {
        private static ImmutableDictionary<string, ProjectTreeFlags> s_configurationFlags = ImmutableDictionary<string, ProjectTreeFlags>.Empty.WithComparers(StringComparer.Ordinal);

        private readonly bool _hasUnresolvedDependency;

        public TargetDependencyViewModel(ITargetFramework targetFramework, bool hasVisibleUnresolvedDependency)
        {
            Caption = targetFramework.FriendlyName;
            Flags = GetCachedFlags(targetFramework);
            _hasUnresolvedDependency = hasVisibleUnresolvedDependency;

            static ProjectTreeFlags GetCachedFlags(ITargetFramework targetFramework)
            {
                return ImmutableInterlocked.GetOrAdd(
                    ref s_configurationFlags,
                    targetFramework.FullName,
                    fullName => DependencyTreeFlags.TargetNode.Add($"$TFM:{fullName}"));
            }
        }

        public string Caption { get; }
        public string? FilePath => null;
        public string? SchemaName => null;
        public string? SchemaItemType => null;
        public ImageMoniker Icon => _hasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
        public ImageMoniker ExpandedIcon => _hasUnresolvedDependency ? ManagedImageMonikers.LibraryWarning : KnownMonikers.Library;
        public ProjectTreeFlags Flags { get; }
    }
}
