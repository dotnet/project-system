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
        /// Get the list of filenames that need to have TempPE libraries created
        /// </summary>
        string[] GetTempPESourceFileNames();

        /// <summary>
        /// Get the XML used by the type resolution service that describes the TempPE library for the specified source file, and triggers the compilation of the library if necessary
        /// </summary>
        /// <param name="sourceFile">A source file that needs a TempPE library created. Must be one of the values returned from <see cref="GetTempPESourceFileNames"/>.</param>
        /// <returns>An XML string formatted the same as the return from <see cref="VSLangProj.BuildManager.BuildDesignTimeOutput(string)"/></returns>
        /// <remarks>The XML format is documented at https://docs.microsoft.com/en-us/dotnet/api/vslangproj.buildmanager.builddesigntimeoutput </remarks>
        Task<string> GetTempPEDescriptionXmlAsync(string sourceFile);
        
        /// <summary>
        /// Notifies the TempPEBuildManager that a source file has changed so that it can take any action necessary
        /// </summary>
        /// <param name="sourceFile">The evaluated include of any source file</param>
        Task NotifySourceFileDirtyAsync(string sourceFile);
    }
}
