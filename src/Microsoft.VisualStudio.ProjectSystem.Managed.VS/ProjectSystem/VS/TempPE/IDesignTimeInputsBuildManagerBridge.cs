// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        /// Get the list of design time monikers that need to have TempPE libraries created. Needs to be called on the UI thread.
        /// </summary>
        Task<string[]> GetTempPEMonikersAsync();

        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        Task<string> GetDesignTimeInputXmlAsync(string relativeFileName);
    }
}
