// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Managed.Build
{
    /// <summary>
    /// A counterpart to the <see cref="ImplicitlyTriggeredBuildManager"/> that exposes
    /// whether or not we are currently in an implicitly-triggered build (that is, a
    /// build that is run as an incidental part of F5, Ctrl+F5, running tests, etc.).
    /// This is primarily intended for use by implementations of <see cref="IProjectGlobalPropertiesProvider"/>
    /// to adjust global build properties.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IImplicitlyTriggeredBuildState
    {
        /// <summary>
        /// Indicates if the current build (if any) is implicitly triggered.
        /// </summary>
        bool IsImplicitlyTriggeredBuild { get; }

        /// <summary>
        /// The full paths to any startup projects associated with an implicitly triggered
        /// build.
        /// </summary>
        /// <remarks>
        /// Even if there are designated startup projects in VS, not every implicit build is
        /// associated with those projects. For example, the startup projects are not
        /// relevant when running an implicit build as part of executing or debugging unit
        /// tests.
        /// </remarks>
        ImmutableArray<string> StartupProjectFullPaths { get; }
    }
}
