// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.Telemetry
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUnconfiguredProjectPackageRestoreTelemetryService
    {
        /// <summary>
        /// Posts a telemetry event from a package restore component.
        /// </summary>
        /// <param name="packageRestoreOperationName">The name of the specific package restore operation.</param>
        void PostPackageRestoreEvent(string packageRestoreOperationName);
    }
}
