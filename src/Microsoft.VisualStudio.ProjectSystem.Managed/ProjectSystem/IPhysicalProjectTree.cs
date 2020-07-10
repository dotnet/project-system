// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represents the physical project tree in Solution Explorer.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IPhysicalProjectTree
    {
        /// <summary>
        ///     Gets the service that provides file and folder operations that operate on the physical <see cref="IProjectTree"/>.
        /// </summary>
        IPhysicalProjectTreeStorage TreeStorage { get; }

        /// <summary>
        ///     Gets the most recently published tree, or <see langword="null"/> if it has not yet be published.
        /// </summary>
        IProjectTree? CurrentTree { get; }

        /// <summary>
        ///     Gets the service that manages the tree in Solution Explorer.
        /// </summary>
        IProjectTreeService TreeService { get; }

        /// <summary>
        ///     Gets the project tree provider that creates the Solution Explorer tree.
        /// </summary>
        IProjectTreeProvider TreeProvider { get; }
    }
}
