// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Exposes a VS specific API related to <see cref="IVsContainedLanguage"/>
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IVsContainedLanguageComponentsFactory
    {
        /// <summary>
        ///     Gets an object that represents a host-specific IVsContainedLanguageFactory implementation.
        ///     Note: currently we have only one target framework and IVsHierarchy and itemId is returned as
        ///     they are from the unconfigured project. Later when combined intellisense is implemented, depending
        ///     on implementation we might need to have a logic that returns IVsHierarchy and itemId specific to
        ///     currently active target framework (that's how it was in Dev14's dnx/dotnet project system)
        /// </summary>
        /// <param name="filePath">Path to a file</param>
        /// <param name="hierarchy">Project hierarchy containing given file for current language service</param>
        /// <param name="itemid">item id of the given file</param>
        /// <param name="containedLanguageFactory">an instance of IVsContainedLanguageFactory specific for current language service</param>
        int GetContainedLanguageFactoryForFile(string filePath,
                                               out IVsHierarchy? hierarchy,
                                               out uint itemid,
                                               out IVsContainedLanguageFactory? containedLanguageFactory);
    }
}
