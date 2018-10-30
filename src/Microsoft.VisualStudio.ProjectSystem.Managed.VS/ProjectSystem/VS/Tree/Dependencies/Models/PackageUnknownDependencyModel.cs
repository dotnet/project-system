// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageUnknownDependencyModel : DependencyModel
    {
        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.QuestionMark,
            expandedIcon: KnownMonikers.QuestionMark,
            unresolvedIcon: KnownMonikers.QuestionMark,
            unresolvedExpandedIcon: KnownMonikers.QuestionMark);

        public override DependencyIconSet IconSet => s_iconSet;

        public override string Id => OriginalItemSpec;

        public override int Priority => Dependency.UnresolvedReferenceNodePriority;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public PackageUnknownDependencyModel(
            string path,
            string originalItemSpec,
            string name,
            ProjectTreeFlags flags,
            bool resolved,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(path, originalItemSpec, flags, resolved, isImplicit: false, properties: properties)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Caption = name;
            TopLevel = false;

            if (dependenciesIDs != null)
            {
                DependencyIDs = ImmutableArray.CreateRange(dependenciesIDs);
            }
        }
    }
}
