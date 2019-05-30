// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class AnalyzerDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.AnalyzerSubTreeNodeFlags);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.CodeInformation,
            expandedIcon: KnownMonikers.CodeInformation,
            unresolvedIcon: ManagedImageMonikers.CodeInformationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.CodeInformationWarning);

        private static readonly DependencyIconSet s_implicitIconSet = new DependencyIconSet(
            icon: ManagedImageMonikers.CodeInformationPrivate,
            expandedIcon: ManagedImageMonikers.CodeInformationPrivate,
            unresolvedIcon: ManagedImageMonikers.CodeInformationWarning,
            unresolvedExpandedIcon: ManagedImageMonikers.CodeInformationWarning);

        public override DependencyIconSet IconSet => Implicit ? s_implicitIconSet : s_iconSet;

        public override int Priority => Dependency.AnalyzerNodePriority;

        public override string ProviderType => AnalyzerRuleHandler.ProviderTypeString;

        public override string SchemaItemType => AnalyzerReference.PrimaryDataSourceItemType;

        public override string SchemaName => Resolved ? ResolvedAnalyzerReference.SchemaName : AnalyzerReference.SchemaName;

        public AnalyzerDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(
                path,
                originalItemSpec,
                flags: s_flagCache.Get(resolved, isImplicit),
                resolved,
                isImplicit,
                properties)
        {
            Caption = resolved
                ? System.IO.Path.GetFileNameWithoutExtension(path)
                : path;
        }
    }
}
