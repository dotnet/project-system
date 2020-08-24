// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.Model.RpcContracts
{
    /// <summary>
    /// The main interface between the client and the server.
    /// These operations are async
    /// </summary>
    public interface IBuildLoggerService
    {
        /// <summary>
        /// Returns whether or not the build logging window is currently tracking logs or not
        /// </summary>
        /// <returns>True if build logging window is tracking logs and false otherwise</returns>
        Task<bool> IsLoggingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns whether or not build logging supports roslyn logging
        /// </summary>
        /// <returns>True if build logging supports roslyn logging, false if otherwise</returns>
        Task<bool> SupportsRoslynLoggingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Tell build logging to start tracking logs
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Tell build logging to stop tracking logs
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Tell build logging to clear out all the accumulated logs contained on the server.
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gives the user a log of a requested build
        /// </summary>
        /// <param name="buildID">an ID used to retrieve a unique log for a build</param>
        /// <returns>The log tied to the requested BuildHandle</returns>
        Task<string> GetLogForBuildAsync(int buildID, CancellationToken cancellationToken);

        /// <summary>
        /// Gives the user a requested build
        /// </summary>
        /// <returns>List of summary information of all builds on the server</returns>
        Task<ImmutableList<BuildSummary>> GetAllBuildsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Event that will be invoked whenever build log data has been updated on the server
        /// </summary>
        public event EventHandler DataChanged;
    }
}
