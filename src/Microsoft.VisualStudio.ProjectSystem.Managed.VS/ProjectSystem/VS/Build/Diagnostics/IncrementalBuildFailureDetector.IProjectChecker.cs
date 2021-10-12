// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.UpToDate;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    internal sealed partial class IncrementalBuildFailureDetector
    {
        /// <summary>
        ///   Tracks per-project state.
        /// </summary>
        /// <remarks>
        ///   The parent class is in the global MEF scope. When a project build completes, we need to find
        ///   it's <see cref="IBuildUpToDateCheckProvider"/> and <see cref="IBuildUpToDateCheckValidator"/>.
        ///   Therefore we have a component in each unconfigured project that imports the <see cref="IActiveConfiguredValue{T}"/>
        ///   of each into this component.
        /// </remarks>
        [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
        internal interface IProjectChecker
        {
            /// <summary>
            /// Checks for incremental build failure.
            /// </summary>
            /// <param name="buildAction">The build action being requested.</param>
            /// <param name="telemetryEnabled">Whether failures should be reported via telemetry.</param>
            /// <param name="outputLoggingEnabled">Whether failures should be logged in the output window.</param>
            /// <param name="cancellationToken">A token indicating if the operation has been cancelled.</param>
            /// <returns></returns>
            Task CheckAsync(
                BuildAction buildAction,
                bool telemetryEnabled,
                bool outputLoggingEnabled,
                CancellationToken cancellationToken);
        }
    }
}
