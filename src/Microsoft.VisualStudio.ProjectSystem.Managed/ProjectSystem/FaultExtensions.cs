// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable RS0030 // Do not used banned APIs (we are wrapping them)

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides extensions methods for APIs in the project system that end up filing Watson reports.
    /// </summary>
    internal static class FaultExtensions
    {
        private static readonly ErrorReportSettings s_defaultReportSettings = new ErrorReportSettings(
            eventName: "VisualStudioNonFatalErrors2",
            component: "ManagedProjectSystem",
            reportType: ErrorReportType.Critical,
            submitFlags: ErrorReportSubmitFlags.OutOfProcess | ErrorReportSubmitFlags.NoCloseUI,
            submitUIOptions: ImmutableDictionary.Create<ErrorReportUIType, string>());

        /// <summary>
        ///     Reports the specified fault.
        /// </summary>
        /// <param name="faultHandlerService">
        ///     The <see cref="IProjectFaultHostHandler"/> that should handle the fault.
        /// </param>
        /// <param name="ex">
        ///     Exception containing the fault information.
        ///  </param>
        /// <param name="severity">
        ///     The severity of the failure.
        /// </param>
        /// <param name="project">
        ///     The project related to the failure, if applicable. Can be <see langword="null"/>.
        /// </param>
        public static Task ReportFaultAsync(
            this IProjectFaultHandlerService faultHandlerService,
            Exception ex,
            UnconfiguredProject? project,
            ProjectFaultSeverity severity = ProjectFaultSeverity.Recoverable)
        {
            Requires.NotNull(faultHandlerService, nameof(faultHandlerService));

            return faultHandlerService.HandleFaultAsync(ex, s_defaultReportSettings, severity, project);
        }

        /// <summary>
        ///     Attaches error handling to a task so that if it throws an unhandled exception,
        ///     the error will be reported to the user.
        /// </summary>
        /// <param name="faultHandlerService">
        ///     The <see cref="IProjectFaultHostHandler"/> that should handle the fault.
        /// </param>
        /// <param name="task">
        ///     The task to attach error handling to.
        /// </param>
        /// <param name="project">
        ///     The project related to the failure, if applicable. Can be <see langword="null"/>.
        /// </param>
        /// <param name="severity">
        ///     The severity of the failure.
        /// </param>
        public static void Forget(
            this IProjectFaultHandlerService faultHandlerService,
            Task task,
            UnconfiguredProject? project,
            ProjectFaultSeverity severity = ProjectFaultSeverity.Recoverable)
        {
            Requires.NotNull(faultHandlerService, nameof(faultHandlerService));

            faultHandlerService.RegisterFaultHandler(task, s_defaultReportSettings, severity, project);
        }

        /// <summary>
        ///     Attaches error handling to a task so that if it throws an unhandled exception,
        ///     the error will be reported to the user.
        /// </summary>
        /// <param name="faultHandlerService">
        ///     The <see cref="IProjectFaultHostHandler"/> that should handle the fault.
        /// </param>
        /// <param name="task">
        ///     The task to attach error handling to.
        /// </param>
        /// <param name="project">
        ///     The project related to the failure, if applicable. Can be <see langword="null"/>.
        /// </param>
        /// <param name="severity">
        ///     The severity of the failure.
        /// </param>
        public static void Forget<TResult>(
            this IProjectFaultHandlerService faultHandlerService,
            Task<TResult> task,
            UnconfiguredProject? project,
            ProjectFaultSeverity severity = ProjectFaultSeverity.Recoverable)
        {
            Requires.NotNull(faultHandlerService, nameof(faultHandlerService));

            faultHandlerService.RegisterFaultHandler(task, s_defaultReportSettings, severity, project);
        }

        /// <summary>
        ///      Executes the specified delegate in a safe fire-and-forget manner, prevent the project from 
        ///      closing until it has completed.
        /// </summary>
        /// <param name="threadingService">
        ///     The <see cref="IProjectThreadingService"/> that handles the fork.
        /// </param>
        /// <param name="asyncAction">
        ///      The async delegate to invoke. It is invoked asynchronously with respect to the caller.
        /// </param>
        /// <param name="unconfiguredProject">
        ///     The unconfigured project which the delegate operates on, if applicable. Can be <see langword="null"/>.
        /// </param>
        /// <param name="faultSeverity">
        ///     Suggests to the user how severe the fault is if the delegate throws.
        /// </param>
        /// <param name="options">
        ///     Influences the environment in which the delegate is executed.
        /// </param>
        public static void RunAndForget(
            this IProjectThreadingService threadingService,
            Func<Task> asyncAction,
            UnconfiguredProject? unconfiguredProject,
            ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable,
            ForkOptions options = ForkOptions.Default)
        {
            Requires.NotNull(threadingService, nameof(threadingService));

            // If you do not pass in a project it is not legal to ask the threading service to cancel this operation on project unloading
            if (unconfiguredProject is null)
            {
                options &= ~ForkOptions.CancelOnUnload;
            }

            threadingService.Fork(asyncAction, factory: null, unconfiguredProject: unconfiguredProject, watsonReportSettings: s_defaultReportSettings, faultSeverity: faultSeverity, options: options);
        }

        /// <summary>
        ///     Executes the specified delegate in a safe fire-and-forget manner, prevent the project from 
        ///     closing until it has completed.
        /// </summary>
        /// <param name="threadingService">
        ///     The <see cref="IProjectThreadingService"/> that handles the fork.
        /// </param>
        /// <param name="asyncAction">
        ///     The async delegate to invoke. It is invoked asynchronously with respect to the caller.
        /// </param>
        /// <param name="configuredProject">
        ///     The configured project which the delegate operates on, if applicable. Can be <see langword="null"/>.
        /// </param>
        /// <param name="faultSeverity">
        ///     Suggests to the user how severe the fault is if the delegate throws.
        /// </param>
        /// <param name="options">
        ///     Influences the environment in which the delegate is executed.
        /// </param>
        public static void RunAndForget(
            this IProjectThreadingService threadingService,
            Func<Task> asyncAction,
            ConfiguredProject? configuredProject,
            ProjectFaultSeverity faultSeverity = ProjectFaultSeverity.Recoverable,
            ForkOptions options = ForkOptions.Default)
        {
            Requires.NotNull(threadingService, nameof(threadingService));

            // If you do not pass in a project it is not legal to ask the threading service to cancel this operation on project unloading
            if (configuredProject is null)
            {
                options &= ~ForkOptions.CancelOnUnload;
            }

            threadingService.Fork(asyncAction, factory: null, configuredProject: configuredProject, watsonReportSettings: s_defaultReportSettings, faultSeverity: faultSeverity, options: options);
        }
    }
}
