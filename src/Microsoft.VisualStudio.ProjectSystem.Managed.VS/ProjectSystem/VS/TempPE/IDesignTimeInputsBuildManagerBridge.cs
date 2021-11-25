// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// Provides a bridge from the DesignTimeInputs system to the UI thread, for use by the BuildManager, which is part of VSLangProj
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDesignTimeInputsBuildManagerBridge
    {
        /// <summary>
        /// Get the list of design time monikers that need to have TempPE libraries created.
        /// </summary>
        Task<string[]> GetDesignTimeOutputMonikersAsync();

        /// <summary>
        /// Builds a temporary portable executable (PE) and returns its description in an XML string.
        /// </summary>
        Task<string> BuildDesignTimeOutputAsync(string outputMoniker);
    }
}
