// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class ComDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.ComDependency);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Component,
            expandedIcon: ManagedImageMonikers.Component,
            unresolvedIcon: ManagedImageMonikers.ComponentWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ComponentWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.ComponentPrivate,
            expandedIcon: ManagedImageMonikers.ComponentPrivate,
            unresolvedIcon: ManagedImageMonikers.ComponentWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.ComponentWarning);

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override string ProviderType => ComRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => ComReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedCOMReference.SchemaName : ComReference.SchemaName;

        public ComDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: isResolved
                    ? System.IO.Path.GetFileNameWithoutExtension(path)
                    : path,
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
