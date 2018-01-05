// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    internal interface IProjectConfigurationDimensionsProvider3
    {
        /// <summary>
        ///     Gets the default values for project configuration dimensions for the given unconfigured
        ///     project.
        /// </summary>
        /// <param name="project">
        ///     Unconfigured project.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        ///     Implementors of this method must implement it without evaluating the project and it 
        ///     results are not guaranteed to be accurate.
        /// </remarks>
        Task<IEnumerable<KeyValuePair<string, string>>> GetBestGuessDefaultValuesForDimensionsAsync(UnconfiguredProject project);
    }
}
