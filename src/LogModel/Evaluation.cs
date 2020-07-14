// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Evaluation
    {
        public ImmutableList<Message> Messages { get; }

        public ImmutableList<EvaluatedProject> EvaluatedProjects { get; }

        public Evaluation(ImmutableList<Message> messages, ImmutableList<EvaluatedProject> evaluatedProjects)
        {
            Messages = messages;
            EvaluatedProjects = evaluatedProjects;
        }
    }
}
