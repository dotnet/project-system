// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    ///     Provides members for opening the Project Designer and querying whether it is supported.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IProjectDesignerService
    {
        /// <summary>
        ///     Gets a value indicating whether the current project supports the Project Designer.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if the current project supports the Project Designer; otherwise, <see langword="false"/>.
        /// </value>
        bool SupportsProjectDesigner { get; }

        /// <summary>
        ///     Shows the current project's Project Designer window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="SupportsProjectDesigner"/> is <see langword="false"/>.
        /// </exception>
        Task ShowProjectDesignerAsync();
    }
}
