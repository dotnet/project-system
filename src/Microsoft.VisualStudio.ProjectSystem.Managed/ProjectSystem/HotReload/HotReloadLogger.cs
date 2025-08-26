// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.VisualStudio.ProjectSystem.HotReload;

using LogLevel = Extensions.Logging.LogLevel;

internal sealed class HotReloadLogger(IHotReloadDiagnosticOutputService service, string projectName, string variant, int sessionInstanceId, string categoryName) : ILogger
{
    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);

        service.WriteLine(
            new HotReloadLogMessage(
                verbosity: logLevel switch
                {
                    LogLevel.Trace or LogLevel.Debug => HotReloadVerbosity.Diagnostic,
                    LogLevel.Information => HotReloadVerbosity.Diagnostic,
                    _ => HotReloadVerbosity.Minimal
                },
                message: message,
                projectName,
                variant: variant,
                instanceId: (uint)sessionInstanceId,
                errorLevel: logLevel switch
                {
                    LogLevel.Warning => HotReloadDiagnosticErrorLevel.Warning,
                    LogLevel.Error => HotReloadDiagnosticErrorLevel.Error,
                    _ => HotReloadDiagnosticErrorLevel.Info,
                },
                categoryName),
            CancellationToken.None);

        System.Diagnostics.Debug.WriteLine($"{GetPrefix(logLevel)} {message}");
    }

    public string GetPrefix(LogLevel logLevel)
        => $"{logLevel}: [{projectName} ({variant}#{sessionInstanceId})]";

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => throw new NotImplementedException();
}
