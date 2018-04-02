// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Log
    {
        public Build Build { get; }

        public ImmutableList<Evaluation> Evaluations { get; }

        public Log(Build build, ImmutableList<Evaluation> evaluations)
        {
            Build = build;
            Evaluations = evaluations;
        }
    }
}
