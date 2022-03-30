// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class ProjectDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.ProjectDependency + DependencyTreeFlags.SupportsBrowse,
            unresolved: DependencyTreeFlags.ProjectDependency + DependencyTreeFlags.SupportsBrowse);

        private static readonly DependencyIconSet s_iconSet = new(
            icon: KnownMonikers.Application,
            expandedIcon: KnownMonikers.Application,
            unresolvedIcon: KnownMonikers.ApplicationWarning,
            unresolvedExpandedIcon: KnownMonikers.ApplicationWarning,
            implicitIcon: KnownMonikers.ApplicationPrivate,
            implicitExpandedIcon: KnownMonikers.ApplicationPrivate);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string ProviderType => ProjectRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => ProjectReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedProjectReference.SchemaName : ProjectReference.SchemaName;

        public ProjectDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: System.IO.Path.GetFileNameWithoutExtension(originalItemSpec),
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit).Add($"$ID:{System.IO.Path.GetFileNameWithoutExtension(path)}"),
                isResolved,
                isImplicit,
                properties)
        {
        }
    }
}
