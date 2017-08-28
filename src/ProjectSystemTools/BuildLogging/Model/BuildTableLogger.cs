// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class BuildTableLogger : ILogger
    {
        private static readonly string[] Dimensions = {"Configuration", "Platform", "TargetFramework"};

        private readonly BuildTableDataSource _dataSource;
        private int _projectInstanceId;
        private Build _build;
        private readonly string _logPath;
        private readonly BinaryLogger _binaryLogger;

        public LoggerVerbosity Verbosity { get => LoggerVerbosity.Diagnostic; set {} }

        public string Parameters { get; set; }

        public BuildTableLogger(BuildTableDataSource dataSource)
        {
            _dataSource = dataSource;
            _logPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.binlog");
            _binaryLogger = new BinaryLogger
            {
                Parameters = _logPath,
                Verbosity =  LoggerVerbosity.Diagnostic,
                CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.None
            };
        }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
            _binaryLogger.Initialize(eventSource);
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            if (e.BuildEventContext.ProjectInstanceId != _projectInstanceId)
            {
                return;
            }

            _build.Finish(e.Succeeded, e.Timestamp, _logPath);
            _dataSource.NotifyChange();
        }

        private static IEnumerable<string> GatherDimensions(IDictionary<string, string> globalProperties)
        {
            foreach (var dimension in Dimensions)
            {
                if (globalProperties.TryGetValue(dimension, out var dimensionValue))
                {
                    yield return dimensionValue;
                }
            }
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            // We only want to register the outermost project build
            if (!_dataSource.IsLogging || e.ParentProjectBuildEventContext.ProjectInstanceId != -1)
            {
                return;
            }

            var isDesignTime = e.GlobalProperties.TryGetValue("DesignTimeBuild", out var value) &&
                               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

            var dimensions = GatherDimensions(e.GlobalProperties);

            var build = new Build(Path.GetFileNameWithoutExtension(e.ProjectFile), dimensions.ToArray(), e.TargetNames?.Split(';'), isDesignTime, e.Timestamp);
            _build = build;
            _projectInstanceId = e.BuildEventContext.ProjectInstanceId;
            _dataSource.AddEntry(_build);
        }

        public void Shutdown()
        {
            _binaryLogger.Shutdown();
        }
    }
}
