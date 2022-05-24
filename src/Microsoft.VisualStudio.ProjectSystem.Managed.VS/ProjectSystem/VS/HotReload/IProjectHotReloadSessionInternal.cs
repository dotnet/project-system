// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload
{
    /// <summary>
    /// Internal interface which Hot Reload sessions implement to provide access to the IDeltaApplier
    /// </summary>
    internal interface IProjectHotReloadSessionInternal
    {
        /// <summary>
        /// Returns the delta applier for this session
        /// </summary>
        IDeltaApplier? DeltaApplier { get; }
    }
}
