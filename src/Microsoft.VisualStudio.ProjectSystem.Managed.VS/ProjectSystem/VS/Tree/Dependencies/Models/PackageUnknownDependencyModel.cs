// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal class PackageUnknownDependencyModel : DependencyModel
    {
        public PackageUnknownDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            string name,
            ProjectTreeFlags flags,
            bool resolved,
            IImmutableDictionary<string, string> properties,
            IEnumerable<string> dependenciesIDs)
            : base(providerType, path, originalItemSpec, flags, resolved, isImplicit:false, properties:properties)
        {
            Requires.NotNullOrEmpty(name, nameof(name));

            Name = name;
            Caption = name;
            TopLevel = false;
            Icon = KnownMonikers.QuestionMark;
            ExpandedIcon = Icon;
            UnresolvedIcon = KnownMonikers.QuestionMark;
            UnresolvedExpandedIcon = UnresolvedIcon;
            Priority = Dependency.UnresolvedReferenceNodePriority;

            if (dependenciesIDs != null && dependenciesIDs.Any())
            {
                DependencyIDs = ImmutableList<string>.Empty.AddRange(dependenciesIDs);
            }
        }

        private string _id;
        public override string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = OriginalItemSpec;
                }

                return _id;
            }
        }
    }
}
