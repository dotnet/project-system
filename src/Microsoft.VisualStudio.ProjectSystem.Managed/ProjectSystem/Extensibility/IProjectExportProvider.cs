// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    /// Interface definition for global scope VS MEF component, which helps to get MEF exports from a
    /// project level scope given IVsHierarchy or project file path.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IProjectExportProvider
    {
        /// <summary>
        /// Returns the export for the given project without having to go to the
        /// UI thread. This is the preferred method for getting access to project specific
        /// exports.
        /// </summary>
        /// <exception cref="System.ArgumentException"><paramref name="projectFilePath"/> is <see langword="null" /> or empty.</exception>
        T? GetExport<T>(string projectFilePath) where T : class;
    }
}
