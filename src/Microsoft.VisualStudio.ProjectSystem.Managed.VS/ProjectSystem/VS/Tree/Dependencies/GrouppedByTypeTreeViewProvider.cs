// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Export(typeof(IDependenciesTreeViewProvider))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    [Order(Order)]
    internal class GrouppedByTypeTreeViewProvider : TreeViewProviderBase
    {
        public const int Order = 900;

        [ImportingConstructor]
        public GrouppedByTypeTreeViewProvider(
            IUnconfiguredProjectCommonServices commonServices)
            : base(commonServices.Project)
        {
        }

        public override IProjectTree BuildTree(IProjectTree dependenciesTree, IDependenciesSnapshot snapshot)
        {
            throw new NotImplementedException();
        }

        public override IProjectTree FindByPath(IProjectTree root, string path)
        {
            throw new NotImplementedException();
        }
    }
}
