// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel
{
    public sealed class Log
    {
        public Build Build { get; }

        public ImmutableList<Evaluation> Evaluations { get; }

        public ImmutableList<Exception> Exceptions { get; }

        public Log(Build build, ImmutableList<Evaluation> evaluations, ImmutableList<Exception> exceptions)
        {
            Build = build;
            Evaluations = evaluations;
            Exceptions = exceptions;
        }
    }
}
