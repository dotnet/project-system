// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.Tools.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    internal sealed class Build : IComparable<Build>, IDisposable
    {
        public BuildType BuildType { get; }

        public IEnumerable<string> Dimensions { get; }

        public IEnumerable<string> Targets { get; }

        public DateTime StartTime { get; }

        public TimeSpan Elapsed { get; private set; }

        public BuildStatus Status { get; private set; }

        public string ProjectPath { get; }

        public string LogPath { get; private set; }

        public Build(string projectPath, IEnumerable<string> dimensions, IEnumerable<string> targets, BuildType buildType, DateTime startTime)
        {
            ProjectPath = projectPath;
            Dimensions = dimensions.ToArray();
            Targets = targets?.ToArray() ?? Enumerable.Empty<string>();
            BuildType = buildType;
            StartTime = startTime;
            Status = BuildStatus.Running;
        }

        public void Finish(bool succeeded, DateTime time)
        {
            if (Status != BuildStatus.Running)
            {
                throw new InvalidOperationException();
            }

            Status = succeeded ? BuildStatus.Finished : BuildStatus.Failed;
            Elapsed = time - StartTime;
        }

        public void SetLogPath(string logPath)
        {
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

                case TableKeyNames.BuildType:
                    content = BuildType;
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

            if (other is null)
            {
                return 1;
            }

            var startComparison = StartTime.CompareTo(other.StartTime);
            return startComparison != 0 ? startComparison : string.Compare(ProjectPath, other.ProjectPath, StringComparison.Ordinal);
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
