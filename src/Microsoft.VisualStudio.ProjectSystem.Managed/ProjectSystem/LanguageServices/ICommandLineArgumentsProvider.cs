// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    /// <summary>
    /// Project value data source for instances of <see cref="CommandLineArgumentsSnapshot"/>, which provides ordered
    /// access to command line arguments produced during design-time builds.
    /// </summary>
    /// <remarks>
    /// While this data is available from <c>BuildRuleSource</c>, CPS's collections reorder items based on hash values.
    /// For the language service, the order of arguments is important. This data source produces a snapshot of ordered
    /// command line arguments from the project build snapshot.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface ICommandLineArgumentsProvider : IProjectValueDataSource<CommandLineArgumentsSnapshot>
    {
    }
}
