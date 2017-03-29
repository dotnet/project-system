// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    ///     An implementation of <see cref="IVsErrorListProvider"/> that delegates onto the language
    ///     service so that it de-dup warnings and errors between IntelliSense and build.
    /// </summary>
    [AppliesTo(ProjectCapability.ManagedLanguageService)]
    [Export(typeof(IVsErrorListProvider))]
    [Order(1)] // One less than the CPS version of this class, until they've removed it
    internal partial class LanguageServiceErrorListProvider : IVsErrorListProvider
    {
        private readonly static Task<AddMessageResult> HandledAndStopProcessing = Task.FromResult(AddMessageResult.HandledAndStopProcessing);
        private readonly static Task<AddMessageResult> NotHandled = Task.FromResult(AddMessageResult.NotHandled);
        private readonly ILanguageServiceHost _host;
        private IVsLanguageServiceBuildErrorReporter2 _languageServiceBuildErrorReporter;

        [ImportingConstructor]
        public LanguageServiceErrorListProvider(UnconfiguredProject unconfiguredProject, ILanguageServiceHost host)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(host, nameof(host));

            _host = host;
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
                return await NotHandled.ConfigureAwait(false);

            InitializeBuildErrorReporter();

            bool handled = false;
            if (_languageServiceBuildErrorReporter != null)
            {
                try
                {
                    _languageServiceBuildErrorReporter.ReportError2(details.Message,
                                                                    details.Code,
                                                                    details.Priority,
                                                                    details.LineNumberForErrorList,
                                                                    details.ColumnNumberForErrorList,
                                                                    details.EndLineNumberForErrorList,
                                                                    details.EndColumnNumberForErrorList,
                                                                    details.GetFileFullPath(_host));
                    handled = true;
                }
                catch (NotImplementedException)
                {   // Language Service doesn't handle it, typically because file 
                    // isn't in the project or because it doesn't have line/column
                }
            }

            return handled ? await HandledAndStopProcessing.ConfigureAwait(false) : await NotHandled.ConfigureAwait(false);
        }

        public Task ClearMessageFromTargetAsync(string targetName)
        {
            return Task.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            if (_languageServiceBuildErrorReporter != null)
            {
                _languageServiceBuildErrorReporter.ClearErrors();
            }

            return Task.CompletedTask;
        }

        private void InitializeBuildErrorReporter()
        {
            // We defer grabbing error reporter the until the first build event, because the language service is initialized asynchronously
            if (_languageServiceBuildErrorReporter == null)
            {
                _languageServiceBuildErrorReporter = (IVsLanguageServiceBuildErrorReporter2)_host.HostSpecificErrorReporter;
            }
        }

        /// <summary>
        ///     Attempts to extract the details required by the VS Error List from an MSBuild build event.
        /// </summary>
        /// <param name="eventArgs">The build event.  May be null.</param>
        /// <param name="result">The extracted details, or <c>null</c> if <paramref name="eventArgs"/> was <c>null</c> or of an unrecognized type.</param>
        internal static bool TryExtractErrorListDetails(BuildEventArgs eventArgs, out ErrorListDetails result)
        {
            BuildErrorEventArgs errorMessage;
            BuildWarningEventArgs warningMessage;
            CriticalBuildMessageEventArgs criticalMessage;

            if ((errorMessage = eventArgs as BuildErrorEventArgs) != null)
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

            if ((warningMessage = eventArgs as BuildWarningEventArgs) != null)
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

            if ((criticalMessage = eventArgs as CriticalBuildMessageEventArgs) != null)
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

            result = default(ErrorListDetails);
            return false;
        }
    }
}
