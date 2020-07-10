// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class EvaluatedProject : Node
    {
        public string Name { get; }
        public EvaluatedProfile EvaluationProfile { get; }

        public EvaluatedProject(string name, EvaluatedProfile evaluationProfile, DateTime startTime, DateTime endTime, ImmutableList<Message> messages) :
            base(messages, startTime, endTime, Result.Succeeded)
        {
            Name = name;
            EvaluationProfile = evaluationProfile;
        }
    }
}
