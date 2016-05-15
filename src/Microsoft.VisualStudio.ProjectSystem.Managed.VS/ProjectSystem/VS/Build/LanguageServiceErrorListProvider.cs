// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Composition;
using System.Linq;
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
        private readonly static Task<AddMessageResult> HandledAndStopProcessingTask = Task.FromResult(AddMessageResult.HandledAndStopProcessing);
        private readonly static Task<AddMessageResult> NotHandledTask = Task.FromResult(AddMessageResult.NotHandled);
        private IVsLanguageServiceBuildErrorReporter _languageServiceBuildErrorReporter;

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

            var details = ExtractErrorListDetails(task.BuildEventArgs);
            if (details == null || string.IsNullOrEmpty(details.Code))
            {
                return NotHandledTask;
            }

            InitializeBuildErrorReporter();

            bool handled = false;
            if (_languageServiceBuildErrorReporter != null)
            {
                try
                {
                    _languageServiceBuildErrorReporter.ReportError(
                        task.Text,
                        details.Code,
                        details.Priority,
                        details.LineNumberForErrorList,
                        details.ColumnNumberForErrorList,
                        details.FileFullPath);
                    handled = true;
                }
                catch (NotImplementedException)
                {
                    // Ignore NotImplementedException.
                }
            }

            return handled ? HandledAndStopProcessingTask : NotHandledTask;
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
                    _languageServiceBuildErrorReporter = reportExternalErrors as IVsLanguageServiceBuildErrorReporter;
                }
            }
        }

        /// <summary>
        /// Extracts the details required by the VS Error List from an MSBuild build event.
        /// </summary>
        /// <param name="eventArgs">The build event.  May be null.</param>
        /// <returns>The extracted details, or <c>null</c> if <paramref name="eventArgs"/> was <c>null</c> or of an unrecognized type.</returns>
        internal static ErrorListDetails ExtractErrorListDetails(BuildEventArgs eventArgs)
        {
            BuildErrorEventArgs errorMessage;
            BuildWarningEventArgs warningMessage;
            CriticalBuildMessageEventArgs criticalMessage;

            if ((errorMessage = eventArgs as BuildErrorEventArgs) != null)
            {
                return new ErrorListDetails(eventArgs)
                {
                    ProjectFile = errorMessage.ProjectFile,
                    File = errorMessage.File,
                    Subcategory = errorMessage.Subcategory,
                    LineNumber = errorMessage.LineNumber,
                    ColumnNumber = errorMessage.ColumnNumber,
                    Code = errorMessage.Code,
                    Priority = VSTASKPRIORITY.TP_HIGH,
                };
            }

            if ((warningMessage = eventArgs as BuildWarningEventArgs) != null)
            {
                return new ErrorListDetails(eventArgs)
                {
                    ProjectFile = warningMessage.ProjectFile,
                    File = warningMessage.File,
                    Subcategory = warningMessage.Subcategory,
                    LineNumber = warningMessage.LineNumber,
                    ColumnNumber = warningMessage.ColumnNumber,
                    Code = warningMessage.Code,
                    Priority = VSTASKPRIORITY.TP_NORMAL,
                };
            }

            if ((criticalMessage = eventArgs as CriticalBuildMessageEventArgs) != null)
            {
                return new ErrorListDetails(eventArgs)
                {
                    ProjectFile = criticalMessage.ProjectFile,
                    File = criticalMessage.File,
                    Subcategory = criticalMessage.Subcategory,
                    LineNumber = criticalMessage.LineNumber,
                    ColumnNumber = criticalMessage.ColumnNumber,
                    Code = criticalMessage.Code,
                    Priority = VSTASKPRIORITY.TP_LOW,
                };
            }

            return null;
        }
    }
}
