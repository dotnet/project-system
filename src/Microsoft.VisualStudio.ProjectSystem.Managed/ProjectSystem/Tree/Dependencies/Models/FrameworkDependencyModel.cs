// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal sealed class FrameworkDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.FrameworkDependency);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Framework,
            expandedIcon: ManagedImageMonikers.Framework,
            unresolvedIcon: ManagedImageMonikers.FrameworkWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.FrameworkWarning);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string ProviderType => FrameworkRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => FrameworkReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedFrameworkReference.SchemaName : FrameworkReference.SchemaName;

        public FrameworkDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: originalItemSpec,
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit: false),
                isResolved,
                isImplicit: false,
                properties)
        {
        }
    }
}
