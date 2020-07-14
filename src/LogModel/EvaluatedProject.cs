// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
