// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class SdkDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(
            add: DependencyTreeFlags.SdkDependency +
                 DependencyTreeFlags.SupportsHierarchy);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.Sdk,
            expandedIcon: ManagedImageMonikers.Sdk,
            unresolvedIcon: ManagedImageMonikers.SdkWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.SdkWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.SdkPrivate,
            expandedIcon: ManagedImageMonikers.SdkPrivate,
            unresolvedIcon: ManagedImageMonikers.SdkWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.SdkWarning);

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override int Priority => GraphNodePriority.SdkNodePriority;

        public override string ProviderType => SdkRuleHandler.ProviderTypeString;

        public override string? SchemaItemType => SdkReference.PrimaryDataSourceItemType;

        public override string? SchemaName => Resolved ? ResolvedSdkReference.SchemaName : SdkReference.SchemaName;

        public SdkDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit),
                isResolved,
                isImplicit,
                properties)
        {
            string version = properties.GetStringProperty(ProjectItemMetadata.Version) ?? string.Empty;
            string? baseCaption = new LazyStringSplit(path, ',').FirstOrDefault();
            Caption = (string.IsNullOrEmpty(version) ? baseCaption : $"{baseCaption} ({version})") ?? originalItemSpec;
        }
    }
}
