// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts;
using Microsoft.VisualStudio.ProjectSystem.Tools.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.BackEnd
{
    /// <summary>
    /// Implements IBuildLoggerService that interacts with Codespaces
    /// </summary>
    [Export (typeof(IBuildLoggerService))]
    public sealed class BuildLoggerService : IBuildLoggerService
    {
        private readonly ILoggingDataSource _dataSource;
        private readonly ILoggingController _loggingController;

        [ImportingConstructor]
        public BuildLoggerService(ILoggingDataSource dataSource, ILoggingController loggingController)
        {
            _dataSource = dataSource;
            _loggingController = loggingController;
        }

        public Task<bool> IsLoggingAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_loggingController.IsLogging);
        }

        public Task<bool> SupportsRoslynLoggingAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_dataSource.SupportsRoslynLogging);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dataSource.BuildsUpdated += DataSource_BuildsUpdated;
            _dataSource.Start();
            return Task.CompletedTask;
        }

        private void DataSource_BuildsUpdated(object sender, EventArgs e)
        {
            DataChanged?.Invoke(this, null);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dataSource.Stop();
            _dataSource.BuildsUpdated -= DataSource_BuildsUpdated;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _dataSource.Clear();
            return Task.CompletedTask;
        }

        public Task<string> GetLogForBuildAsync(int buildID, CancellationToken cancellationToken)
        
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_dataSource.GetLogForBuild(buildID));
        }

        public Task<ImmutableList<BuildSummary>> GetAllBuildsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_dataSource.GetAllBuilds());
        }

        /// <summary>
        /// Implemented event for IBuildLoggerService
        /// This event will be invoked whenever ILoggingDataSource's BuildsUpdated event is invoked
        /// </summary>
        public event EventHandler DataChanged;
    }
}
