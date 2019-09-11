// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IDesignTimeInputsCompiler : IDisposable
    {
        /// <summary>
        /// Gets the XML that describes a TempPE DLL, including building it if necessary
        /// </summary>
        /// <param name="relativeFileName">A project relative path to a source file that is a design time input</param>
        /// <param name="tempPEOutputPath">The path in which to place the TempPE DLL if one is created</param>
        /// <param name="sharedInputs">The list of shared inputs to be included in the TempPE DLL</param>
        /// <returns>An XML description of the TempPE DLL for the specified file</returns>
        Task<string> GetDesignTimeInputXmlAsync(string relativeFileName, string tempPEOutputPath, System.Collections.Immutable.ImmutableHashSet<string> sharedInputs);
    }
}
