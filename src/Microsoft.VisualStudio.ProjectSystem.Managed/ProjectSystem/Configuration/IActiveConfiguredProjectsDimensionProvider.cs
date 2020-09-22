// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    /// <summary>
    ///     Indicates that a dimension provided by a <see cref="IProjectConfigurationDimensionsProvider"/> instance
    ///     should participate in calculating the active project configurations for <see cref="IActiveConfiguredProjectsProvider"/>.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IActiveConfiguredProjectsDimensionProvider
    {
        /// <summary>
        ///     Gets the name of the dimension that should participate in calculating the active project configurations.
        /// </summary>
        string DimensionName { get; }
    }
}
