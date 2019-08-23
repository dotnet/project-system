// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    ///     Provides the implicitly active dimensions from a list of dimension names. 
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IImplicitlyActiveDimensionProvider
    {
        /// <summary>
        ///     Returns the implicitly active dimension names from the specified dimension names.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="dimensionNames"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///     NOTE: The returned order matches the order in which the dimension names and values 
        ///     should be displayed to the user.
        /// </remarks>
        IEnumerable<string> GetImplicitlyActiveDimensions(IEnumerable<string> dimensionNames);
    }
}
