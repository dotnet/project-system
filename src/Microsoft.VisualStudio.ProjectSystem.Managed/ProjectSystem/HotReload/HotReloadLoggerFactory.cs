// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

internal sealed class HotReloadLoggerFactory(IHotReloadDiagnosticOutputService service, string projectName, string targetFramework, int sessionInstanceId) : ILoggerFactory
{
    public void Dispose()
    {
    }

    public string ProjectName
        => projectName;

    public string TargetFramework
        => targetFramework;

    public ILogger CreateLogger(string categoryName)
        => new HotReloadLogger(
            service,
            projectName,
            variant: targetFramework,
            sessionInstanceId,
            categoryName);

    public void AddProvider(ILoggerProvider provider)
        => throw new NotImplementedException();
}
