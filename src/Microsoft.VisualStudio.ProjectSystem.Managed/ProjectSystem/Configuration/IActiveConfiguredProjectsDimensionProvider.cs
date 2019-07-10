// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        string DimensionName
        {
            get;
        }
    }
}
