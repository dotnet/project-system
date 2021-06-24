// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Provides a default <see cref="ILaunchProfile"/> to be displayed when no profiles are provided.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface IDefaultLaunchProfileProvider
    {
        /// <summary>
        /// Gets the default <see cref="ILaunchProfile"/>. Return null to remove the default profile.
        /// </summary>
        ILaunchProfile? CreateDefaultProfile();
    }
}
