// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    ///     Hosts the "active" <see cref="IWorkspaceProjectContext"/> for an <see cref="UnconfiguredProject"/> 
    ///     and provides consumers access to it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The "active" <see cref="IWorkspaceProjectContext"/> for an <see cref="UnconfiguredProject"/> is the one associated 
    ///         with the solution's active configuration and represents the context that is used for features that are not yet aware 
    ///         of multi-targeting projects, including Razor, compiler errors/warnings that come from build (#4034) and Edit-and-Continue. 
    ///         All other features should live in the <see cref="ConfiguredProject"/> scope and import the current 
    ///         <see cref="IWorkspaceProjectContextHost"/> .
    ///     </para>
    ///     <para>
    ///         NOTE: This is distinct from the "active" context for the editor which is tracked via <see cref="IActiveEditorContextTracker"/>.
    ///     </para>
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IActiveWorkspaceProjectContextHost : IWorkspaceProjectContextHost
    {
    }
}
