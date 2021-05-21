// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Moq;

namespace Microsoft.VisualStudio.Mocks
{
    internal static class IProjectBuildSnapshotFactory
    {
        public static IProjectBuildSnapshot Create()
        {
            var buildSnapshotMock = new Mock<IProjectBuildSnapshot>();

            IImmutableDictionary<string, IImmutableList<KeyValuePair<string, IImmutableDictionary<string, string>>>>
                targetOutputs = 
                    ImmutableDictionary<string, IImmutableList<KeyValuePair<string, IImmutableDictionary<string, string>>>>.Empty
                        .Add("CompileDesignTime", ImmutableArray<KeyValuePair<string, IImmutableDictionary<string, string>>>.Empty);

            buildSnapshotMock.SetupGet(s => s.TargetOutputs)
                .Returns(targetOutputs);

            return buildSnapshotMock.Object;
        }
    }
}
