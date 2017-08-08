// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model
{
    [Export(typeof(IBuildTableDataSource))]
    internal sealed class BuildTableDataSource: ITableDataSource, ITableEntriesSnapshotFactory, IBuildTableDataSource
    {
        private const string BuildDataSourceDisplayName = "Build Data Source";
        private const string BuildTableDataSourceIdentifier = nameof(BuildTableDataSourceIdentifier);
        private const string BuildTableDataSourceSourceTypeIdentifier = nameof(BuildTableDataSourceSourceTypeIdentifier);

        private readonly object _gate = new object();

        private ITableManager _manager;
        private ITableDataSink _tableDataSink;
        private BuildTableEntriesSnapshot _lastSnapshot;
        private ImmutableList<Build> _entries = ImmutableList<Build>.Empty;
        private readonly Dictionary<int, Build> _currentBuilds = new Dictionary<int, Build>();
        private BuildOperation _currentOperation = BuildOperation.DesignTime;
        private DateTime _currentOperationTime = DateTime.Now;

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool IsLogging { get; private set; }

        public int CurrentVersionNumber { get; private set; }

        public LoggerVerbosity Verbosity { get; set; }

        public string Parameters { get; set; }
        
        public ITableManager Manager
        {
            get => _manager;
            set
            {
                _manager?.RemoveSource(this);
                _manager = value;
                _manager?.AddSource(this);
            }
        }

        public void Start() => IsLogging = true;

        public void Stop() => IsLogging = false;

        public void Clear()
        {
            _entries = ImmutableList<Build>.Empty;
            CurrentVersionNumber++;
            NotifyChange();
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            _tableDataSink = sink;

            _tableDataSink.AddFactory(this, removeAllFactories: true);
            _tableDataSink.IsStable = true;

            return this;
        }

        public void Dispose() => Manager = null;

        private void NotifyChange() => _tableDataSink.FactorySnapshotChanged(this);

        public ITableEntriesSnapshot GetCurrentSnapshot()
        {
            lock (_gate)
            {
                if (_lastSnapshot?.VersionNumber != CurrentVersionNumber)
                {
                    _lastSnapshot = new BuildTableEntriesSnapshot(_entries, CurrentVersionNumber);
                }

                return _lastSnapshot;
            }
        }

        public ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            lock (_gate)
            {
                if (_lastSnapshot?.VersionNumber == versionNumber)
                {
                    return _lastSnapshot;
                }

                if (versionNumber == CurrentVersionNumber)
                {
                    return GetCurrentSnapshot();
                }
            }

            // We didn't have this version.  Notify the sinks that something must have changed
            // so that they call back into us with the latest version.
            NotifyChange();
            return null;
        }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.ProjectStarted += ProjectStarted;
            eventSource.ProjectFinished += ProjectFinished;
        }

        private void ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            lock (_gate)
            {
                if (!_currentBuilds.TryGetValue(e.BuildEventContext.ProjectInstanceId, out var build))
                {
                    return;
                }

                build.Finish(e.Succeeded, e.Timestamp);
                _currentBuilds[e.BuildEventContext.ProjectInstanceId] = null;
            }
        }

        private void ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            if (!IsLogging || e.ParentProjectBuildEventContext.ProjectInstanceId >= 0)
            {
                return;
            }

            lock (_gate)
            {
                var build = new Build(_currentOperation, _currentOperationTime,
                    Path.GetFileNameWithoutExtension(e.ProjectFile), Enumerable.Empty<string>(),
                    e.TargetNames?.Split(';'), e.Timestamp);
                _currentBuilds[e.BuildEventContext.ProjectInstanceId] = build;
                _entries = _entries.Add(build);
                CurrentVersionNumber++;
                NotifyChange();
            }
        }

        public void Shutdown()
        {
        }

        private static BuildOperation ActionToOperation(uint dwAction)
        {
            var action = (VSSOLNBUILDUPDATEFLAGS)dwAction;

            switch (action & VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_MASK)
            {
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN:
                    return BuildOperation.Clean;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD:
                    return BuildOperation.Build;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_FORCE_UPDATE:
                    return BuildOperation.Rebuild;
                case VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_DEPLOY:
                    return BuildOperation.Deploy;
            }

            var action2 = (VSSOLNBUILDUPDATEFLAGS2)dwAction;

            switch (action2 & (VSSOLNBUILDUPDATEFLAGS2)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_MASK)
            {
                case VSSOLNBUILDUPDATEFLAGS2.SBF_OPERATION_PUBLISH:
                    return BuildOperation.Publish;
                case VSSOLNBUILDUPDATEFLAGS2.SBF_OPERATION_PUBLISHUI:
                    return BuildOperation.PublishUI;
                default:
                    return BuildOperation.Unknown;
            }
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_QueryDelayFirstUpdateAction(out int pfDelay) => pfDelay = 0;

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginFirstUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_EndLastUpdateAction()
        {
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_BeginUpdateAction(uint dwAction)
        {
            _currentOperation = ActionToOperation(dwAction);
            _currentOperationTime = DateTime.Now;
        }

        void IVsUpdateSolutionEvents4.UpdateSolution_EndUpdateAction(uint dwAction)
        {
            _currentOperation = BuildOperation.DesignTime;
            _currentOperationTime = DateTime.Now;
        }

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchBegin()
        {
        }

        void IVsUpdateSolutionEvents4.OnActiveProjectCfgChangeBatchEnd()
        {
        }
    }
}
