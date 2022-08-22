// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

[ProjectSystemContract(ProjectSystemContractScope.Global, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.OneOrZero)]
internal interface ILanguageServiceHostEnvironment
{
    /// <summary>
    /// Gets whether the language service host should be enabled in this environment.
    /// </summary>
    /// <remarks>
    /// For example, within VS, the language service should not be initialised when running
    /// in command line mode.
    /// </remarks>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken);
}
