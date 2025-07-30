// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload.Components.DeltaApplier;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

/// <summary>
/// Internal interface which Hot Reload sessions implement to provide access to the IDeltaApplier
/// </summary>
internal interface IProjectHotReloadSessionInternal : IProjectHotReloadSession, IManagedHotReloadAgent5
{
    /// <summary>
    /// Returns the delta applier for this session
    /// </summary>
    IDeltaApplier? DeltaApplier { get; }
}
