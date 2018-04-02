// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
