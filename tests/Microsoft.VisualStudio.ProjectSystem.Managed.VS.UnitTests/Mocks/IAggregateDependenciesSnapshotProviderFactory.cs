// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE file in the project root for more information.

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
