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
    /// An implemenation of <see cref="IVsErrorListProvider"/> in unconfigured project scope
    /// to take over populating of VB/C# compiler errors and warnings in the error list.
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
            this.CodeModelProviders = new OrderPrecedenceImportCollection<ICodeModelProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, unconfiguredProject);
        }

        /// <summary>
        /// Gets all the code model providers in the system.
        /// </summary>
        [ImportMany]
        private OrderPrecedenceImportCollection<ICodeModelProvider> CodeModelProviders
        {
            get;
            set;
        }

        /// <summary>
        /// <see cref="IVsErrorListProvider.SuspendRefresh"/>
        /// </summary>
        public void SuspendRefresh()
        {
        }

        /// <summary>
        /// <see cref="IVsErrorListProvider.ResumeRefresh"/>
        /// </summary>
        public void ResumeRefresh()
        {
        }

        /// <summary>
        /// <see cref="IVsErrorListProvider.AddMessageAsync"/>
        /// </summary>
        public Task<AddMessageResult> AddMessageAsync(TargetGeneratedTask task)
        {
            Requires.NotNull(task, nameof(task));

            var details = ExtractErrorListDetails(task.BuildEventArgs);
            if (details == null || string.IsNullOrEmpty(details.Code))
            {
                return NotHandledTask;
            }

            // Defer acquisition of languageServiceBuildErrorReporter until the build event comes, because Language Service integration is done
            // asynchronously and we do not want to block this component's initialization on that part.
            if (this._languageServiceBuildErrorReporter == null && this.CodeModelProviders.Any())
            {
                var projectWithIntellisense = this.CodeModelProviders.First().Value as IProjectWithIntellisense;
                if (projectWithIntellisense != null && projectWithIntellisense.IntellisenseProject != null)
                {
                    // TODO: VB's IVsIntellisenseProject::GetExternalErrorReporter() does not return the correct instance which should be QIed from the inner IVbCompilerProject (BUG 1024166),
                    // so this code works on C# only.
                    IVsReportExternalErrors vsReportExternalErrors;
                    if (projectWithIntellisense.IntellisenseProject.GetExternalErrorReporter(out vsReportExternalErrors) == 0)
                    {
                        this._languageServiceBuildErrorReporter = vsReportExternalErrors as IVsLanguageServiceBuildErrorReporter;
                    }
                }
            }

            bool handled = false;

            if (this._languageServiceBuildErrorReporter != null)
            {
                try
                {
                    this._languageServiceBuildErrorReporter.ReportError(
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

        /// <summary>
        /// <see cref="IVsErrorListProvider.ClearMessageFromTargetAsync"/>
        /// </summary>
        public Task ClearMessageFromTargetAsync(string targetName)
        {
            return TplExtensions.CompletedTask;
        }

        /// <summary>
        /// <see cref="IVsErrorListProvider.ClearAllAsync"/>
        /// </summary>
        public Task ClearAllAsync()
        {
            if (this._languageServiceBuildErrorReporter != null)
            {
                this._languageServiceBuildErrorReporter.ClearErrors();
            }

            return TplExtensions.CompletedTask;
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
