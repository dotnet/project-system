// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build.Diagnostics
{
    internal sealed partial class IncrementalBuildFailureDetector
    {
        /// <summary>
        ///   Tracks per-project state.
        /// </summary>
        /// <remarks>
        ///   The parent class is in the global MEF scope. Projects are built within the context of a specific
        ///   configuration. We use exports of this component to perform logic at the configuration scope.
        /// </remarks>
        [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
        internal interface IProjectChecker
        {
            /// <summary>
            /// Checks for incremental build failure.
            /// </summary>
            void OnProjectBuildCompleted();
        }
    }
}
