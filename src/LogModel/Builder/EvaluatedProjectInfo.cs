// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    internal sealed class EvaluatedProjectInfo : BaseInfo
    {
        public string Name { get; }
        public EvaluatedProfileInfo EvaluationProfile { get; private set; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }

        public EvaluatedProjectInfo(string name, DateTime startTime)
        {
            Name = name;
            StartTime = startTime;
        }

        public void EndEvaluatedProject(EvaluatedProfileInfo evaluationProfile, DateTime endTime)
        {
            EvaluationProfile = evaluationProfile;
            EndTime = endTime;
        }
    }
}
