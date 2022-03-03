// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Represents a marker interface for types that apply changes to <see cref="IWorkspaceProjectContext"/> instances.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IWorkspaceContextHandler
    {
        /// <summary>
        ///     Initializes the handler with the specified <see cref="IWorkspaceProjectContext"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="Initialize(IWorkspaceProjectContext)"/> has already been called.
        /// </exception>
        void Initialize(IWorkspaceProjectContext context);
    }
}
