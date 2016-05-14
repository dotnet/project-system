// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    using System;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.ProjectSystem.VS.Build;
    using Microsoft.VisualStudio.ProjectSystem.VS.Implementation.Package;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// An implemenation of <see cref="IVsErrorListProvider"/> in unconfigured project scope
    /// to take over populating of VB/C# compiler errors and warnings in the error list.
    /// </summary>
    [AppliesTo("( " + ProjectCapabilities.VB + " | " + ProjectCapabilities.CSharp + " ) & !ManagedLang & " + ProjectCapabilities.LanguageService)]
    [Export(typeof(IVsErrorListProvider))]
    [Order(1000)]
    internal class LanguageServiceErrorListProvider : IVsErrorListProvider
    {
        /// <summary>
        /// Cache the task so that we do not need to allocate it on each invocation.
        /// </summary>
        private readonly static Task<AddMessageResult> HandledAndStopProcessingTask = Task.FromResult(AddMessageResult.HandledAndStopProcessing);

        /// <summary>
        /// Cache the task so that we do not need to allocate it on each invocation.
        /// </summary>
        private readonly static Task<AddMessageResult> NotHandledTask = Task.FromResult(AddMessageResult.NotHandled);

        /// <summary>
        /// The instance of <see cref="IVsLanguageServiceBuildErrorReporter"/> being obtained from Language Service.
        /// </summary>
        private IVsLanguageServiceBuildErrorReporter languageServiceBuildErrorReporter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageServiceErrorListProvider"/> class.
        /// </summary>
        [ImportingConstructor]
        private LanguageServiceErrorListProvider(UnconfiguredProject unconfiguredProject)
        {
            this.CodeModelProviders = new OrderPrecedenceImportCollection<ICodeModelProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, unconfiguredProject);
        }

        /// <summary>
        /// Gets all the code model providers in the system.
        /// </summary>
        [ImportMany]
        private OrderPrecedenceImportCollection<ICodeModelProvider> CodeModelProviders { get; set; }

        #region IVsErrorListProvider Members

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

            var details = task.BuildEventArgs.ExtractErrorListDetails();
            if (details == null || string.IsNullOrEmpty(details.Code))
            {
                return NotHandledTask;
            }

            // Defer acquisition of languageServiceBuildErrorReporter until the build event comes, because Language Service integration is done
            // asynchronously and we do not want to block this component's initialization on that part.
            if (this.languageServiceBuildErrorReporter == null && this.CodeModelProviders.Any())
            {
                var projectWithIntellisense = this.CodeModelProviders.First().Value as IProjectWithIntellisense;
                if (projectWithIntellisense != null && projectWithIntellisense.IntellisenseProject != null)
                {
                    // TODO: VB's IVsIntellisenseProject::GetExternalErrorReporter() does not return the correct instance which should be QIed from the inner IVbCompilerProject (BUG 1024166),
                    // so this code works on C# only.
                    IVsReportExternalErrors vsReportExternalErrors;
                    if (ErrorHandler.Succeeded(projectWithIntellisense.IntellisenseProject.GetExternalErrorReporter(out vsReportExternalErrors)))
                    {
                        this.languageServiceBuildErrorReporter = vsReportExternalErrors as IVsLanguageServiceBuildErrorReporter;
                    }
                }
            }

            bool handled = false;

            if (this.languageServiceBuildErrorReporter != null)
            {
                try
                {
                    this.languageServiceBuildErrorReporter.ReportError(
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
            if (this.languageServiceBuildErrorReporter != null)
            {
                this.languageServiceBuildErrorReporter.ClearErrors();
            }

            return TplExtensions.CompletedTask;
        }

        #endregion
    }
}
