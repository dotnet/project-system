// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class AnalyzerDependencyModel : DependencyModel
    {
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
            Icon = isImplicit ? ManagedImageMonikers.CodeInformationPrivate : KnownMonikers.CodeInformation;
            ExpandedIcon = Icon;
            UnresolvedIcon = ManagedImageMonikers.CodeInformationWarning;
            UnresolvedExpandedIcon = UnresolvedIcon;
        }
    }
}
