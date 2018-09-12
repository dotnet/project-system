// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class AnalyzerDependencyModel : DependencyModel
    {
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

        public AnalyzerDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            ProjectTreeFlags flags,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit, properties)
        {
            if (Resolved)
            {
                Caption = System.IO.Path.GetFileNameWithoutExtension(Name);
                SchemaName = ResolvedAnalyzerReference.SchemaName;
            }
            else
            {
                Caption = Path;
                SchemaName = AnalyzerReference.SchemaName;
            }

            SchemaItemType = AnalyzerReference.PrimaryDataSourceItemType;
            Priority = Dependency.AnalyzerNodePriority;
            IconSet = isImplicit ? s_implicitIconSet : s_iconSet;
        }
    }
}
