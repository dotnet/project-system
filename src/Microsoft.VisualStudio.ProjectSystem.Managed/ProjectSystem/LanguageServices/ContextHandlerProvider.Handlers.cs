// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    partial class ContextHandlerProvider
    {
        private class Handlers
        {
            public readonly ImmutableArray<(IEvaluationHandler handler, string evaluationRuleName)> EvaluationHandlers;
            public readonly ImmutableArray<ICommandLineHandler> CommandLineHandlers;

            public Handlers(ImmutableArray<(IEvaluationHandler handler, string evaluationRuleName)> evaluationHandlers, ImmutableArray<ICommandLineHandler> commandLineHandlers)
            {
                EvaluationHandlers = evaluationHandlers;
                CommandLineHandlers = commandLineHandlers;
            }
        }
    }
}
