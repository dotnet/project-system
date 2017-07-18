// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    partial class ContextHandlerProvider
    {
        private class Handlers
        {
            public readonly ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> EvaluationHandlers;
            public readonly ImmutableArray<ICommandLineHandler> CommandLineHandlers;

            public Handlers(ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> evaluationHandlers, ImmutableArray<ICommandLineHandler> commandLineHandlers)
            {
                EvaluationHandlers = evaluationHandlers;
                CommandLineHandlers = commandLineHandlers;
            }
        }
    }
}
