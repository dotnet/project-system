﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

internal sealed partial class Workspace
{
    private sealed class AsyncVsLanguageServiceBuildErrorReporterWrapper(IVsLanguageServiceBuildErrorReporter2 reporter)
        : IAsyncVsLanguageServiceBuildErrorReporter
    {
        public Task ClearErrorsAsync()
        {
            reporter.ClearErrors();
            return Task.CompletedTask;
        }

        public Task<bool> TryReportErrorAsync(
            string errorMessage,
            string errorId,
            VSTASKPRIORITY taskPriority,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            string fileName)
        {
            try
            {
                reporter.ReportError2(
                    errorMessage, errorId, taskPriority, startLine, startColumn, endLine, endColumn, fileName);
                return TaskResult.True;
            }
            catch(NotImplementedException)
            {
                return TaskResult.False;
            }
        }
    }
}
