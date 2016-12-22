// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    /// Utility class for allowing for testing of code that needs to access the msbuild lock, and also be testable.
    /// </summary>
    internal interface IMsBuildAccessor
    {
        /// <summary>
        /// Gets the XML for a given unconfigured project.
        /// </summary>
        Task<string> GetProjectXmlAsync();

        /// <summary>
        /// Saves the given xml to the project file.
        /// </summary>
        Task SaveProjectXmlAsync(string toSave);
    }
}
