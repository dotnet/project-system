// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

/// <summary>
/// Allows <see cref="IProjectHotReloadSessionCallback"/> to specify whether to suppresses application of deltas 
/// as a workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2570151
/// </summary>
public interface ISuppressDeltaApplication
{
    bool SuppressDeltaApplication { get; }
}
