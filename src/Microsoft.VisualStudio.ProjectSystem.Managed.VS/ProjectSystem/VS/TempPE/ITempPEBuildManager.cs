// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// Provides TempPE build information and services to the build manager
    /// </summary>
    internal interface ITempPEBuildManager
    {
        /// <summary>
        /// Get the list of design time monikers that need to have TempPE libraries created
        /// </summary>
        string[] GetTempPEMonikers();

        /// <summary>
        /// Get the XML used by the type resolution service that describes the TempPE library for the specified input moniker, and triggers the compilation of the library if necessary
        /// </summary>
        /// <param name="moniker">One of the monikers returned from <see cref="GetTempPEMonikers"/> for which a TempPE library should be produced.</param>
        /// <returns>An XML string formatted the same as the return from <see cref="VSLangProj.BuildManager.BuildDesignTimeOutput(string)"/></returns>
        /// <remarks>The XML format is documented at https://docs.microsoft.com/en-us/dotnet/api/vslangproj.buildmanager.builddesigntimeoutput </remarks>
        Task<string> GetTempPEDescriptionXmlAsync(string moniker);
    }
}
