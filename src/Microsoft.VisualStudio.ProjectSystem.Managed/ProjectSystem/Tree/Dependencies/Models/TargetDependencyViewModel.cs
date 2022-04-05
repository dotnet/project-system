// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal sealed class TargetDependencyViewModel : IDependencyViewModel
    {
        private static ImmutableDictionary<string, ProjectTreeFlags> s_configurationFlags = ImmutableDictionary<string, ProjectTreeFlags>.Empty.WithComparers(StringComparer.Ordinal);

        private readonly DiagnosticLevel _diagnosticLevel;

        public TargetDependencyViewModel(TargetFramework targetFramework, DiagnosticLevel diagnosticLevel)
        {
            Caption = targetFramework.TargetFrameworkAlias;
            Flags = GetCachedFlags(targetFramework);
            _diagnosticLevel = diagnosticLevel;

            static ProjectTreeFlags GetCachedFlags(TargetFramework targetFramework)
            {
                return ImmutableInterlocked.GetOrAdd(
                    ref s_configurationFlags,
                    targetFramework.TargetFrameworkAlias,
                    fullName => DependencyTreeFlags.TargetNode
                        .Add($"$TFM:{fullName}")
                        .Add(ProjectTreeFlags.Common.VirtualFolder));
            }
        }

        public string Caption { get; }
        public string? FilePath => null;
        public string? SchemaName => null;
        public string? SchemaItemType => null;
        public ImageMoniker Icon => _diagnosticLevel == DiagnosticLevel.None ? KnownMonikers.Library : KnownMonikers.LibraryWarning;
        public ImageMoniker ExpandedIcon => _diagnosticLevel == DiagnosticLevel.None ? KnownMonikers.Library : KnownMonikers.LibraryWarning;
        public ProjectTreeFlags Flags { get; }
    }
}
