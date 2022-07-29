// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// A marker interface for types that apply changes to <see cref="IWorkspaceProjectContext"/> instances.
    /// </summary>
    /// <remarks>
    /// Valid types are <see cref="ICommandLineHandler"/>, <see cref="IProjectEvaluationHandler"/>
    /// and <see cref="ISourceItemsHandler"/>. Implementations of this interface are only invoked
    /// when implementing one of these specific subtypes.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IWorkspaceUpdateHandler
    {
    }
}
