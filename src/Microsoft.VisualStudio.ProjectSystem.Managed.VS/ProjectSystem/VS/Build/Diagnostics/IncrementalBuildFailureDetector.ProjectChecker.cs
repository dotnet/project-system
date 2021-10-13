// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    internal sealed partial class IncrementalBuildFailureDetector
    {
        /// <summary>
        /// Unconfigured project scoped data related to incremental build failure detection.
        /// </summary>
        [Export(typeof(IProjectChecker))]
        private sealed class ProjectChecker : IProjectChecker
        {
            private readonly UnconfiguredProject _project;
            private readonly IActiveConfiguredValue<IBuildUpToDateCheckProvider> _upToDateCheckProvider;
            private readonly IActiveConfiguredValue<IBuildUpToDateCheckValidator> _upToDateCheckValidator;
            private readonly IProjectAsynchronousTasksService _projectAsynchronousTasksService;

            [ImportMany]
            private OrderPrecedenceImportCollection<IIncrementalBuildFailureReporter> Reporters { get; }

            [ImportingConstructor]
            public ProjectChecker(
                UnconfiguredProject project,
                IVsUIService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
                IActiveConfiguredValue<IBuildUpToDateCheckProvider> upToDateCheckProvider,
                IActiveConfiguredValue<IBuildUpToDateCheckValidator> upToDateCheckValidator,
                [Import(ExportContractNames.Scopes.UnconfiguredProject)] IProjectAsynchronousTasksService projectAsynchronousTasksService,
                IVsUIService<SVsOutputWindow, IVsOutputWindow> outputWindow)
            {
                _project = project;
                _upToDateCheckProvider = upToDateCheckProvider;
                _upToDateCheckValidator = upToDateCheckValidator;
                _projectAsynchronousTasksService = projectAsynchronousTasksService;
                
                Reporters = new OrderPrecedenceImportCollection<IIncrementalBuildFailureReporter>(projectCapabilityCheckProvider: project);
            }

            public void OnProjectBuildCompleted(BuildAction buildAction)
            {
                _project.Services.ThreadingPolicy.RunAndForget(
                    async () =>
                    {
                        await TaskScheduler.Default;

                        await CheckAsync(_projectAsynchronousTasksService.UnloadCancellationToken);
                    },
                    unconfiguredProject: _project);

                return;

                async Task CheckAsync(CancellationToken cancellationToken)
                {
                    var sw = Stopwatch.StartNew();

                    if (!await _upToDateCheckProvider.Value.IsUpToDateCheckEnabledAsync(cancellationToken))
                    {
                        // The fast up-to-date check has been disabled. We can't know the reason why.
                        // We currently do not flag errors in this case, so stop processing immediately.
                        return;
                    }

                    List<IIncrementalBuildFailureReporter>? reporters = await GetEnabledReportersAsync();

                    if (reporters is null)
                    {
                        // No reporter is enabled, so return immediately without checking anything
                        return;
                    }

                    (bool isUpToDate, string? failureReason) = await _upToDateCheckValidator.Value.ValidateUpToDateAsync(buildAction, cancellationToken);

                    if (isUpToDate)
                    {
                        // The project is up-to-date, as expected. Nothing more to do.
                        return;
                    }

                    Assumes.NotNull(failureReason);

                    TimeSpan checkDuration = sw.Elapsed;

                    foreach (IIncrementalBuildFailureReporter reporter in reporters)
                    {
                        await reporter.ReportFailureAsync(failureReason, checkDuration, cancellationToken);
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
