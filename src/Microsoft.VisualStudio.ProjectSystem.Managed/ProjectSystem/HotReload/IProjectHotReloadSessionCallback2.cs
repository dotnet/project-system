// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal interface IProjectHotReloadSessionCallback2 : IProjectHotReloadSessionCallback
{
    UnconfiguredProject? Project { get; }

    Process? Process { get; }

    IProjectHotReloadSession? Session { get; }
}
