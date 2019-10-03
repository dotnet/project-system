// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides methods for querying and testing the current project's capabilities.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectCapabilitiesService
    {   // This interface introduced just so that we can mock checks for capabilities, 
        // to avoid static state and call context data that we cannot influence

        /// <summary>
        ///     Returns a value indicating whether the current project has the specified capability
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="capability"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="capability"/> is an empty string ("").
        /// </exception>
        bool Contains(string capability);
    }
}
