// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Wrapper around the active debug framework to provide a single implementation of what is considered the active framework. If there is
    /// only one framework.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IActiveDebugFrameworkServices
    {
        /// <summary>
        /// Returns the set of frameworks in the order defined in msbuild. If not multi-targeting it returns null.
        /// </summary>
        Task<List<string>?> GetProjectFrameworksAsync();

        /// <summary>
        /// Sets the value of the active debugging target framework property.
        /// </summary>
        Task SetActiveDebuggingFrameworkPropertyAsync(string activeFramework);

        /// <summary>
        /// Returns the value of the property, or empty string/null if the property has never been set.
        /// </summary>
        Task<string?> GetActiveDebuggingFrameworkPropertyAsync();

        /// <summary>
        /// Returns the configured project which represents the active framework. This is valid whether multi-targeting or not.
        /// </summary>
        Task<ConfiguredProject?> GetConfiguredProjectForActiveFrameworkAsync();
    }
}
