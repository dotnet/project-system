// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    ///     Provides an implementation of <see cref="ILaunchSettingsProvider"/> with an
    ///     additional method <see cref="GetLaunchSettingsFilePathAsync"/> for retrieving
    ///     the launch settings file.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface ILaunchSettingsProvider2 : ILaunchSettingsProvider
    {
        /// <summary>
        ///     Gets the full path to the launch settings file, typically located under
        ///     "Properties\launchSettings.json" or "My Project\launchSettings.json" of
        ///     the project directory.
        /// </summary>
        Task<string> GetLaunchSettingsFilePathAsync();
    }
}
