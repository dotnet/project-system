// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Internal.VisualStudio.Shell.Interop;
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
        internal const string LspPullDiagnosticsFeatureFlagName = "Lsp.PullDiagnostics";

        private readonly UnconfiguredProject _project;
        private readonly IWorkspaceWriter _workspaceWriter;

        private readonly AsyncLazy<bool> _isLspPullDiagnosticsEnabled;

        /// <remarks>
        /// <see cref="UnconfiguredProject"/> must be imported in the constructor in order for scope of this class' export to be correct.
        /// </remarks>
        [ImportingConstructor]
        public LanguageServiceErrorListProvider(
            UnconfiguredProject project,
            IWorkspaceWriter workspaceWriter,
            IVsService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            JoinableTaskContext joinableTaskContext)
        {
            _project = project;
            _workspaceWriter = workspaceWriter;

            _isLspPullDiagnosticsEnabled = new AsyncLazy<bool>(async () =>
            {
                IVsFeatureFlags? service = await featureFlagsService.GetValueAsync();
                return service.IsFeatureEnabled(LspPullDiagnosticsFeatureFlagName, defaultValue: false);
            }, joinableTaskContext.Factory);
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

            // When this feature flag is enabled, build diagnostics will be published by CPS and should not be passed to roslyn.
            bool isLspPullDiagnosticsEnabled = await _isLspPullDiagnosticsEnabled.GetValueAsync();
            if (isLspPullDiagnosticsEnabled)
            {
                return AddMessageResult.NotHandled;
            }

            bool handled = false;

            if (await _workspaceWriter.IsEnabledAsync())
            {
                handled = await _workspaceWriter.WriteAsync(workspace =>
                {
                    var errorReporter = (IVsLanguageServiceBuildErrorReporter2)workspace.HostSpecificErrorReporter;

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
            }

            return handled ? AddMessageResult.HandledAndStopProcessing : AddMessageResult.NotHandled;
        }

        public Task ClearMessageFromTargetAsync(string targetName)
        {
            return Task.CompletedTask;
        }

        public async Task ClearAllAsync()
        {
            if (await _workspaceWriter.IsEnabledAsync())
            {
                await _workspaceWriter.WriteAsync(workspace =>
                {
                    ((IVsLanguageServiceBuildErrorReporter2)workspace.HostSpecificErrorReporter).ClearErrors();

                    return Task.CompletedTask;
                });
            }
        }

        /// <summary>
        ///     Attempts to extract the details required by the VS Error List from an MSBuild build event.
        /// </summary>
        /// <param name="eventArgs">The build event.  May be null.</param>
        /// <param name="result">The extracted details, or <see langword="null"/> if <paramref name="eventArgs"/> was <see langword="null"/> or of an unrecognized type.</param>
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
