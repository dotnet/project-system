// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal sealed class FrameworkDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.FrameworkDependency + DependencyTreeFlags.SupportsFolderBrowse,
            unresolved: DependencyTreeFlags.FrameworkDependency);

        private static readonly DependencyIconSet s_iconSet = new(
            icon: KnownMonikers.Framework,
            expandedIcon: KnownMonikers.Framework,
            unresolvedIcon: KnownMonikers.FrameworkWarning,
            unresolvedExpandedIcon: KnownMonikers.FrameworkWarning,
            implicitIcon: KnownMonikers.FrameworkPrivate,
            implicitExpandedIcon: KnownMonikers.FrameworkPrivate);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string ProviderType => FrameworkRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => FrameworkReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedFrameworkReference.SchemaName : FrameworkReference.SchemaName;

        public FrameworkDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: originalItemSpec,
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit),
                isResolved,
                isImplicit,
                properties)
        {
        }
    }
}
