// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class Build : IComparable<Build>, IDisposable
    {
        public bool DesignTime { get; }

        public IEnumerable<string> Dimensions { get; }

        public IEnumerable<string> Targets { get; }

        public DateTime StartTime { get; }

        public TimeSpan Elapsed { get; private set; }

        public BuildStatus Status { get; private set; }

        public string ProjectPath { get; }

        public string LogPath { get; private set; }

        public string Filename => $"{Path.GetFileNameWithoutExtension(ProjectPath)}_{Dimensions.Aggregate((c, n) => string.IsNullOrEmpty(n) ? c : $"{c}_{n}")}_{(DesignTime ? "design" : "")}_{StartTime:s}.binlog".Replace(':', '_');

        public Build(string projectPath, IEnumerable<string> dimensions, IEnumerable<string> targets, bool designTime, DateTime startTime)
        {
            ProjectPath = projectPath;
            Dimensions = dimensions.ToArray();
            Targets = targets?.ToArray() ?? Enumerable.Empty<string>();
            DesignTime = designTime;
            StartTime = startTime;
        }

        public void Finish(bool succeeded, DateTime time, string logPath)
        {
            if (Status != BuildStatus.Running)
            {
                throw new InvalidOperationException();
            }

            Status = succeeded ? BuildStatus.Finished : BuildStatus.Failed;
            Elapsed = time - StartTime;
            LogPath = logPath;
        }

        public bool TryGetValue(string keyName, out object content)
        {
            switch (keyName)
            {
                case TableKeyNames.Dimensions:
                    content = Dimensions;
                    break;

                case TableKeyNames.Targets:
                    content = Targets;
                    break;

                case TableKeyNames.Elapsed:
                    content = Elapsed;
                    break;

                case TableKeyNames.DesignTime:
                    content = DesignTime;
                    break;

                case TableKeyNames.Status:
                    content = Status;
                    break;

                case StandardTableKeyNames.ProjectName:
                    content = Path.GetFileNameWithoutExtension(ProjectPath);
                    break;

                case TableKeyNames.ProjectType:
                    content = Path.GetExtension(ProjectPath);
                    break;

                case TableKeyNames.StartTime:
                    content = StartTime;
                    break;

                case TableKeyNames.LogPath:
                    content = LogPath;
                    break;

                case TableKeyNames.Filename:
                    content = Filename;
                    break;

                default:
                    content = null;
                    break;
            }

            return content != null;
        }

        public int CompareTo(Build other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            var startComparison = StartTime.CompareTo(other.StartTime);
            return startComparison != 0 ? startComparison : String.Compare(ProjectPath, other.ProjectPath, StringComparison.Ordinal);
        }

        public void Dispose()
        {
            if (LogPath == null)
            {
                return;
            }

            var logPath = LogPath;
            LogPath = null;
            try
            {
                File.Delete(logPath);
            }
            catch
            {
                // If it fails, it fails...
            }
        }
    }
}
