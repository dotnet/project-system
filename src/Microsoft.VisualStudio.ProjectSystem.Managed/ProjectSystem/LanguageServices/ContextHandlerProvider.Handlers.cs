// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    partial class ContextHandlerProvider
    {
        private class Handlers
        {
            public readonly ImmutableArray<ExportLifetimeContext<AbstractContextHandler>> All;
            public readonly ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> EvaluationHandlers;
            public readonly ImmutableArray<ICommandLineHandler> CommandLineHandlers;

            public Handlers(ImmutableArray<ExportLifetimeContext<AbstractContextHandler>> all, ImmutableArray<(IEvaluationHandler Value, string EvaluationRuleName)> evaluationHandlers, ImmutableArray<ICommandLineHandler> commandLineHandlers)
            {
                All = all;
                EvaluationHandlers = evaluationHandlers;
                CommandLineHandlers = commandLineHandlers;
            }
        }
    }
}
