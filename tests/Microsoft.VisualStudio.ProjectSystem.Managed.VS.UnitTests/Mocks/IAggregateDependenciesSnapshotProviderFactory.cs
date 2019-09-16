// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IAggregateDependenciesSnapshotProviderFactory
    {
        public static IAggregateDependenciesSnapshotProvider Create()
        {
            return Mock.Of<IAggregateDependenciesSnapshotProvider>();
        }
    }
}
