// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    ///     An implementation of <see cref="IVsErrorListProvider"/> that delegates onto the language
    ///     service so that it de-dup warnings and errors between IntelliSense and build.
    /// </summary>
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    [Export(typeof(IVsErrorListProvider))]
    [Order(Order.Default)]
    internal partial class LanguageServiceErrorListProvider : IVsErrorListProvider
    {
        private readonly UnconfiguredProject _project;
        private readonly IActiveWorkspaceProjectContextHost _projectContextHost;

        /// <remarks>
        /// <see cref="UnconfiguredProject"/> must be imported in the constructor in order for scope of this class' export to be correct.
        /// </remarks>
        [ImportingConstructor]
        public LanguageServiceErrorListProvider(UnconfiguredProject project, IActiveWorkspaceProjectContextHost projectContextHost)
        {
            _project = project;
            _projectContextHost = projectContextHost;
        }

        public void SuspendRefresh()
        {
        }

        public void ResumeRefresh()
        {
        }

        public Task<AddMessageResult> AddMessageAsync(TargetGeneratedError error)
        {
            Requires.NotNull(error, nameof(error));

            return AddMessageCoreAsync(error);
        }

        private async Task<AddMessageResult> AddMessageCoreAsync(TargetGeneratedError error)
        {
            // We only want to pass compiler, analyzers, etc to the language 
            // service, so we skip tasks that do not have a code
            if (!TryExtractErrorListDetails(error.BuildEventArgs, out ErrorListDetails details) || string.IsNullOrEmpty(details.Code))
                return AddMessageResult.NotHandled;

            bool handled = await _projectContextHost.OpenContextForWriteAsync(accessor =>
            {
                var errorReporter = (IVsLanguageServiceBuildErrorReporter2)accessor.HostSpecificErrorReporter;

                try
                {
                    errorReporter.ReportError2(details.Message,
                                               details.Code,
                                               details.Priority,
                                               details.LineNumberForErrorList,
                                               details.ColumnNumberForErrorList,
                                               details.EndLineNumberForErrorList,
                                               details.EndColumnNumberForErrorList,
                                               details.GetFileFullPath(_project.FullPath));
                    return TaskResult.True;
                }
                catch (NotImplementedException)
                {   // Language Service doesn't handle it, typically because file 
                    // isn't in the project or because it doesn't have line/column
                }

                return TaskResult.False;
            });

            return handled ? AddMessageResult.HandledAndStopProcessing : AddMessageResult.NotHandled;
        }

        public Task ClearMessageFromTargetAsync(string targetName)
        {
            return Task.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            return _projectContextHost.OpenContextForWriteAsync(accessor =>
            {
                ((IVsLanguageServiceBuildErrorReporter2)accessor.HostSpecificErrorReporter).ClearErrors();

                return Task.CompletedTask;
            });

        }

        /// <summary>
        ///     Attempts to extract the details required by the VS Error List from an MSBuild build event.
        /// </summary>
        /// <param name="eventArgs">The build event.  May be null.</param>
        /// <param name="result">The extracted details, or <c>null</c> if <paramref name="eventArgs"/> was <c>null</c> or of an unrecognized type.</param>
        internal static bool TryExtractErrorListDetails(BuildEventArgs eventArgs, out ErrorListDetails result)
        {
            if (eventArgs is BuildErrorEventArgs errorMessage)
            {
                result = new ErrorListDetails()
                {
                    ProjectFile = errorMessage.ProjectFile,
                    File = errorMessage.File,
                    LineNumber = errorMessage.LineNumber,
                    EndLineNumber = errorMessage.EndLineNumber,
                    ColumnNumber = errorMessage.ColumnNumber,
                    EndColumnNumber = errorMessage.EndColumnNumber,
                    Code = errorMessage.Code,
                    Message = errorMessage.Message,
                    Priority = VSTASKPRIORITY.TP_HIGH,
                };

                return true;
            }

            if (eventArgs is BuildWarningEventArgs warningMessage)
            {
                result = new ErrorListDetails()
                {
                    ProjectFile = warningMessage.ProjectFile,
                    File = warningMessage.File,
                    LineNumber = warningMessage.LineNumber,
                    EndLineNumber = warningMessage.EndLineNumber,
                    ColumnNumber = warningMessage.ColumnNumber,
                    EndColumnNumber = warningMessage.EndColumnNumber,
                    Code = warningMessage.Code,
                    Message = warningMessage.Message,
                    Priority = VSTASKPRIORITY.TP_NORMAL,
                };

                return true;
            }

            if (eventArgs is CriticalBuildMessageEventArgs criticalMessage)
            {
                result = new ErrorListDetails()
                {
                    ProjectFile = criticalMessage.ProjectFile,
                    File = criticalMessage.File,
                    LineNumber = criticalMessage.LineNumber,
                    EndLineNumber = criticalMessage.EndLineNumber,
                    ColumnNumber = criticalMessage.ColumnNumber,
                    EndColumnNumber = criticalMessage.EndColumnNumber,
                    Code = criticalMessage.Code,
                    Message = criticalMessage.Message,
                    Priority = VSTASKPRIORITY.TP_LOW,
                };

                return true;
            }

            result = default;
            return false;
        }
    }
}
