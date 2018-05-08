// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal class DependenciesChanges : IDependenciesChanges
    {
        private HashSet<IDependencyModel> Added { get; } = new HashSet<IDependencyModel>();
        private HashSet<IDependencyModel> Removed { get; } = new HashSet<IDependencyModel>();

        public IImmutableList<IDependencyModel> AddedNodes => ImmutableList.CreateRange(Added);
        public IImmutableList<IDependencyModel> RemovedNodes => ImmutableList.CreateRange(Removed);

        public void IncludeAddedChange(IDependencyModel model)
        {
            Added.Add(model);
        }

        public void ExcludeAddedChange(IDependencyModel model)
        {
            Added.Remove(model);
        }

        public void IncludeRemovedChange(IDependencyModel model)
        {
            Removed.Add(model);
        }

        public void ExcludeRemovedChange(IDependencyModel model)
        {
            Removed.Remove(model);
        }
    }
}
