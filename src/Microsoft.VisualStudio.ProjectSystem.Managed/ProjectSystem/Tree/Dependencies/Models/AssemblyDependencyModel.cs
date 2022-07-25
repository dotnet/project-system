// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal class AssemblyDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new(
            resolved: DependencyTreeFlags.AssemblyDependency + DependencyTreeFlags.SupportsBrowse,
            unresolved: DependencyTreeFlags.AssemblyDependency);

        private static readonly DependencyIconSet s_iconSet = new(
            icon: KnownMonikers.Reference,
            expandedIcon: KnownMonikers.Reference,
            unresolvedIcon: KnownMonikers.ReferenceWarning,
            unresolvedExpandedIcon: KnownMonikers.ReferenceWarning,
            implicitIcon: KnownMonikers.ReferencePrivate,
            implicitExpandedIcon: KnownMonikers.ReferencePrivate);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string ProviderType => AssemblyRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => AssemblyReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedAssemblyReference.SchemaName : AssemblyReference.SchemaName;

        public AssemblyDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                caption: GetCaption(path, isResolved, properties),
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit),
                isResolved,
                isImplicit,
                properties)
        {
        }

        private static string GetCaption(string path, bool isResolved, IImmutableDictionary<string, string> properties)
        {
            if (isResolved)
            {
                string? fusionName = properties.GetStringProperty(ResolvedAssemblyReference.FusionNameProperty);

                return fusionName is null ? path : new AssemblyName(fusionName).Name;
            }

            return path;
        }
    }
}
