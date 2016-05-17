// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    ///     An implementation of <see cref="IVsErrorListProvider"/> that delegates onto the language
    ///     service so that it de-dup warnings and errors between IntelliSense and build.
    /// </summary>
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    [Export(typeof(IVsErrorListProvider))]
    [Order(1)] // One less than the CPS version of this class, until they've removed it
    internal partial class LanguageServiceErrorListProvider : IVsErrorListProvider
    {
        private readonly static Task<AddMessageResult> HandledAndStopProcessing = Task.FromResult(AddMessageResult.HandledAndStopProcessing);
        private readonly static Task<AddMessageResult> NotHandled = Task.FromResult(AddMessageResult.NotHandled);
        private IVsLanguageServiceBuildErrorReporter2 _languageServiceBuildErrorReporter;

        [ImportingConstructor]
        public LanguageServiceErrorListProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            ProjectsWithIntellisense = new OrderPrecedenceImportCollection<IProjectWithIntellisense>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, unconfiguredProject);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IProjectWithIntellisense> ProjectsWithIntellisense
        {
            get;
        }

        public void SuspendRefresh()
        {
        }

        public void ResumeRefresh()
        {
        }

        public Task<AddMessageResult> AddMessageAsync(TargetGeneratedTask task)
        {
            Requires.NotNull(task, nameof(task));

            return AddMessageAsyncCore(task);
        }

        private async Task<AddMessageResult> AddMessageAsyncCore(TargetGeneratedTask task)
        {
            // We only want to pass compiler, analyzers, etc to the language 
            // service, so we skip tasks that do not have a code
            ErrorListDetails details;
            if (!TryExtractErrorListDetails(task.BuildEventArgs, out details) || string.IsNullOrEmpty(details.Code))
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
                                                                    details.FileFullPath);
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
            return TplExtensions.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            if (_languageServiceBuildErrorReporter != null)
            {
                _languageServiceBuildErrorReporter.ClearErrors();
            }

            return TplExtensions.CompletedTask;
        }

        private void InitializeBuildErrorReporter()
        {
            // We defer grabbing error reporter the until the first build event, because the language service is initialized asynchronously
            if (_languageServiceBuildErrorReporter == null && ProjectsWithIntellisense.Count > 0)
            {
                var project = ProjectsWithIntellisense.First();

                // TODO: VB's IVsIntellisenseProject::GetExternalErrorReporter() does not return the correct instance which should be QIed from the inner IVbCompilerProject (BUG 1024166),
                // so this code works on C# only.
                IVsReportExternalErrors reportExternalErrors;
                if (project.Value.IntellisenseProject.GetExternalErrorReporter(out reportExternalErrors) == 0)
                {
                    _languageServiceBuildErrorReporter = reportExternalErrors as IVsLanguageServiceBuildErrorReporter2;
                }
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
