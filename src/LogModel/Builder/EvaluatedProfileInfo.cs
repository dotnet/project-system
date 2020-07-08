// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
