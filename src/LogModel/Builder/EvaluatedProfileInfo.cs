// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class EvaluatedProfileInfo
    {
        public ImmutableArray<EvaluatedPassInfo> Passes { get; }
        public TimeInfo EvaluationTimeInfo { get; }

        public EvaluatedProfileInfo(IEnumerable<EvaluatedPassInfo> passes, TimeInfo evalutionTimeInfo)
        {
            Passes = passes.ToImmutableArray();
            EvaluationTimeInfo = evalutionTimeInfo;
        }
    }
}
