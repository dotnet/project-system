// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
