// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal interface IDependenciesSnapshot 
    {
        string ProjectPath { get; }
        ITargetFramework ActiveTarget { get; }
        IImmutableDictionary<ITargetFramework, ITargetedDependenciesSnapshot> Targets { get; }
        bool HasUnresolvedDependency { get; }
        IDependency FindDependency(string id);
    }
}
