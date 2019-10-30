// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageUnknownDependencyModel : DependencyModel
    {
        private static readonly DependencyFlagCache s_flagCache = new DependencyFlagCache(add: DependencyTreeFlags.NuGetDependency);

        private static readonly DependencyIconSet s_iconSet = new DependencyIconSet(
            icon: KnownMonikers.QuestionMark,
            expandedIcon: KnownMonikers.QuestionMark,
            unresolvedIcon: KnownMonikers.QuestionMark,
            unresolvedExpandedIcon: KnownMonikers.QuestionMark);

        public override IImmutableList<string> DependencyIDs { get; }

        public override DependencyIconSet IconSet => s_iconSet;

        public override string Name { get; }

        public override int Priority => GraphNodePriority.UnresolvedReference;

        public override string ProviderType => PackageRuleHandler.ProviderTypeString;

        public PackageUnknownDependencyModel(
            string path,
            string originalItemSpec,
            string name,
            bool isResolved,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(
                path,
                originalItemSpec,
                flags: s_flagCache.Get(isResolved, isImplicit: false),
                isResolved,
                isImplicit: false,
                properties,
                isTopLevel: false)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Caption = name;

            if (dependenciesIDs != null)
            {
                DependencyIDs = ImmutableArray.CreateRange(dependenciesIDs);
            }
            else
            {
                DependencyIDs = ImmutableList<string>.Empty;
            }
        }
    }
}
