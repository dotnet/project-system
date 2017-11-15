// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class EvaluationLogger : ILogger
    {
        private readonly BuildTableDataSource _dataSource;
        private readonly Dictionary<int, Build> _evaluations = new Dictionary<int, Build>();

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Diagnostic; set { } }

        public string Parameters { get; set; }

        public EvaluationLogger(BuildTableDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.StatusEventRaised += StatusEvent;
        }

        private void StatusEvent(object sender, BuildStatusEventArgs args)
        {
            switch (args)
            {
                case ProjectEvaluationStartedEventArgs evaluationStarted:
                    {
                        if (!_dataSource.IsLogging)
                        {
                            return;
                        }

                        var build = new Build(evaluationStarted.ProjectFile, Array.Empty<string>(), new[] {"<Evaluation>"}, 
                            false, args.Timestamp);
                        _evaluations[evaluationStarted.BuildEventContext.EvaluationId] = build;
                        _dataSource.AddEntry(build);
                    }
                    break;

                case ProjectEvaluationFinishedEventArgs evaluationFinished:
                    {
                        if (_evaluations.TryGetValue(evaluationFinished.BuildEventContext.EvaluationId, out var build))
                        {
                            build.Finish(true, args.Timestamp, null);
                            _dataSource.NotifyChange();
                        }
                    }
                    break;
            }
        }

        public void Shutdown()
        {
        }
    }
}
