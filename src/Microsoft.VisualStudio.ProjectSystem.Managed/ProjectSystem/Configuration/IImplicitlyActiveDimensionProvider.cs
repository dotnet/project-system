// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
