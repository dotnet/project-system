// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI;
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

        public string SourceTypeIdentifier => BuildTableDataSourceSourceTypeIdentifier;

        public string Identifier => BuildTableDataSourceIdentifier;

        public string DisplayName => BuildDataSourceDisplayName;

        public bool IsLogging { get; private set; }

        public int CurrentVersionNumber { get; private set; }

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
            foreach (var build in _entries)
            {
                build.Dispose();
            }
            _entries = ImmutableList<Build>.Empty;
            CurrentVersionNumber++;
            NotifyChange();
        }

        public ILogger CreateLogger() => new BuildTableLogger(this);

        public IDisposable Subscribe(ITableDataSink sink)
        {
            _tableDataSink = sink;

            _tableDataSink.AddFactory(this, removeAllFactories: true);
            _tableDataSink.IsStable = true;

            return this;
        }

        public void Dispose()
        {
            foreach (var build in _entries)
            {
                build.Dispose();
            }
            _entries = ImmutableList<Build>.Empty;
            Manager = null;
        }

        public void NotifyChange()
        {
            CurrentVersionNumber++;
            _tableDataSink.FactorySnapshotChanged(this);
        }

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

        public void AddEntry(Build build)
        {
            _entries = _entries.Add(build);
            NotifyChange();
        }
    }
}
