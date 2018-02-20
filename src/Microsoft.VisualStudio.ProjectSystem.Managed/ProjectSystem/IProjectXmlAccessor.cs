// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Utility class for allowing for testing of code that needs to access the msbuild lock, and also be testable.
    /// </summary>
    internal interface IProjectXmlAccessor
    {
        /// <summary>
        /// Gets the evaluated property value for the specified property.
        /// </summary>
        /// <param name="unconfiguredProject"></param>
        /// <param name="propertyName"></param>
        /// <returns>Value of the property. Null if the property does not exist in the project.</returns>
        Task<string> GetEvaluatedPropertyValue(UnconfiguredProject unconfiguredProject, string propertyName);

        /// <summary>
        /// Executes the specified operation in a write lock.
        /// </summary>
        /// <param name="action">Operation to execute</param>
        /// <returns>A task for the async operation.</returns>
        Task ExecuteInWriteLock(Action<ProjectRootElement> action);
    }
}
