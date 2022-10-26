// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    internal sealed partial class IncrementalBuildFailureDetector
    {
        /// <summary>
        /// Configured project scoped data related to incremental build failure detection.
        /// </summary>
        [AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
        [Export(typeof(IProjectChecker))]
        private sealed class ProjectChecker : IProjectChecker
        {
            private readonly ConfiguredProject _configuredProject;
            private readonly IBuildUpToDateCheckValidator _upToDateCheckValidator;
            private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;

            [ImportMany]
            private OrderPrecedenceImportCollection<IIncrementalBuildFailureReporter> Reporters { get; }

            [ImportingConstructor]
            public ProjectChecker(
                ConfiguredProject configuredProject,
                IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
                IBuildUpToDateCheckValidator upToDateCheckValidator,
                [Import(ExportContractNames.Scopes.ConfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService)
            {
                _configuredProject = configuredProject;
                _upToDateCheckValidator = upToDateCheckValidator;
                _projectAsynchronousTasksService = projectAsynchronousTasksService;

                Reporters = new OrderPrecedenceImportCollection<IIncrementalBuildFailureReporter>(projectCapabilityCheckProvider: configuredProject);
            }

            public void OnProjectBuildCompleted()
            {
                _configuredProject.Services.ThreadingPolicy.RunAndForget(
                    () => CheckAsync(_projectAsynchronousTasksService.UnloadCancellationToken),
                    configuredProject: _configuredProject,
                    options: ForkOptions.StartOnThreadPool | ForkOptions.CancelOnUnload | ForkOptions.NoAssistanceMask);

                return;

                async Task CheckAsync(CancellationToken cancellationToken)
                {
                    var sw = Stopwatch.StartNew();

                    if (_upToDateCheckValidator is IBuildUpToDateCheckProvider provider)
                    {
                        if (!await provider.IsUpToDateCheckEnabledAsync(cancellationToken))
                        {
                            // The fast up-to-date check has been disabled. We can't know the reason why.
                            // We currently do not flag errors in this case, so stop processing immediately.
                            return;
                        }
                    }

                    List<IIncrementalBuildFailureReporter>? reporters = await GetEnabledReportersAsync();

                    if (reporters is null)
                    {
                        // No reporter is enabled, so return immediately without checking anything
                        return;
                    }

                    (bool isUpToDate, string? failureReason, string? failureDescription) = await _upToDateCheckValidator.ValidateUpToDateAsync(cancellationToken);

                    if (isUpToDate)
                    {
                        // The project is up-to-date, as expected. Nothing more to do.
                        return;
                    }

                    Assumes.NotNull(failureReason);
                    Assumes.NotNull(failureDescription);

                    TimeSpan checkDuration = sw.Elapsed;

                    foreach (IIncrementalBuildFailureReporter reporter in reporters)
                    {
                        await reporter.ReportFailureAsync(failureReason, failureDescription, checkDuration, cancellationToken);
                    }

                    return;

                    async Task<List<IIncrementalBuildFailureReporter>?> GetEnabledReportersAsync()
                    {
                        List<IIncrementalBuildFailureReporter>? reporters = null;

                        foreach (IIncrementalBuildFailureReporter reporter in Reporters.ExtensionValues())
                        {
                            if (await reporter.IsEnabledAsync(cancellationToken))
                            {
                                reporters ??= new();
                                reporters.Add(reporter);
                            }
                        }

                        return reporters;
                    }
                }
            }
        }
    }
}
