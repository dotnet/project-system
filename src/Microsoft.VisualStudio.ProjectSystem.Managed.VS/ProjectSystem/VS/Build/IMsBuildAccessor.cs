// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Utility class for allowing for testing of code that needs to access the msbuild lock, and also be testable.
    /// </summary>
    internal interface IMsBuildAccessor
    {
        /// <summary>
        /// Gets the XML for a given unconfigured project.
        /// </summary>
        Task<string> GetProjectXmlAsync();

        /// <summary>
        /// Saves the given xml to the project file.
        /// </summary>
        Task SaveProjectXmlAsync(string toSave);

        /// <summary>
        /// Registers an EventHandler for the ProjectXmlChanged event on the msbuild model for the given unconfigured project.
        /// </summary>
        Task SubscribeProjectXmlChangedEventAsync(UnconfiguredProject unconfiguredProject, EventHandler<ProjectXmlChangedEventArgs> handler);

        /// <summary>
        /// Removes an EventHandler for the ProjectXmlChanged event on the msbuild model for the given unconfigured project.
        /// </summary>
        /// <param name="unconfiguredProject"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task UnsubscribeProjectXmlChangedEventAsync(UnconfiguredProject unconfiguredProject, EventHandler<ProjectXmlChangedEventArgs> handler);

        /// <summary>
        /// Runs a given task inside either a read lock or a write lock.
        /// </summary>
        Task RunLockedAsync(bool writeLock, Func<Task> task);
    }
}
