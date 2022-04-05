// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class ComDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.ComDependency + DependencyTreeFlags.SupportsBrowse,
            unresolved: DependencyTreeFlags.ComDependency);

        private static readonly DependencyIconSet s_iconSet = new(
            icon: KnownMonikers.COM,
            expandedIcon: KnownMonikers.COM,
            unresolvedIcon: KnownMonikers.COMWarning,
            unresolvedExpandedIcon: KnownMonikers.COMWarning,
            implicitIcon: KnownMonikers.COMPrivate,
            implicitExpandedIcon: KnownMonikers.COMPrivate);

        public override DependencyIconSet IconSet => s_iconSet;

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
