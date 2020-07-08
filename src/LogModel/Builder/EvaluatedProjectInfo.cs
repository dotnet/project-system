// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
