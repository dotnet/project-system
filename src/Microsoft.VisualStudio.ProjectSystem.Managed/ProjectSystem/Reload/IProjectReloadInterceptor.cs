// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Interface to intercept project reload.
    /// </summary>
    internal interface IProjectReloadInterceptor
    {
        /// <summary>
        /// Intercept project reload when the project file changes.
        /// </summary>
        /// <param name="oldProperties">Before snapshot of the project properties.</param>
        /// <param name="newProperties">After snapshot of the project properties.</param>
        /// <returns>
        /// Return <see cref="ProjectReloadResult.NoAction"/> to continue normal reload flow.
        /// Otherwise, return a specific <see cref="ProjectReloadResult"/>.
        /// </returns>
        /// <remarks>This method is called within a write lock of the project file.</remarks>
        ProjectReloadResult InterceptProjectReload(ImmutableArray<ProjectPropertyElement> oldProperties, ImmutableArray<ProjectPropertyElement> newProperties);
    }
}