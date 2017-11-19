// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class ProjectLogger : LoggerBase
    {
        private static readonly string[] Dimensions = {"Configuration", "Platform", "TargetFramework"};

        private readonly bool _isDesignTime;
        private int _projectInstanceId;
        private readonly string _logPath;
        private readonly BinaryLogger _binaryLogger;
        private Build _build;

        public ProjectLogger(BuildTableDataSource dataSource, bool isDesignTime) :
            base(dataSource)
        {
            _logPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.binlog");
            _binaryLogger = new BinaryLogger
            {
                Parameters = _logPath,
                Verbosity = LoggerVerbosity.Diagnostic,
                CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.None
            };
            _isDesignTime = isDesignTime;
        }

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
            _binaryLogger.Initialize(eventSource);
        }

        public override void Shutdown()
        {
            _binaryLogger.Shutdown();
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            if (e.BuildEventContext.ProjectInstanceId != _projectInstanceId)
            {
                return;
            }

            _build.Finish(e.Succeeded, e.Timestamp, _logPath);
            DataSource.NotifyChange();
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
            if (!DataSource.IsLogging || e.ParentProjectBuildEventContext.ProjectInstanceId != -1)
            {
                return;
            }

            var dimensions = GatherDimensions(e.GlobalProperties);

            var build = new Build(e.ProjectFile, dimensions.ToArray(), e.TargetNames?.Split(';'), _isDesignTime ? BuildType.DesignTimeBuild : BuildType.Build, e.Timestamp);
            _build = build;
            _projectInstanceId = e.BuildEventContext.ProjectInstanceId;
            DataSource.AddEntry(_build);
        }
    }
}
