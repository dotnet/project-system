// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.WPF;

[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension, Cardinality = Composition.ImportCardinality.ExactlyOne)]
internal interface IApplicationXamlFileAccessor
{
    Task<string?> GetStartupUriAsync();
    Task SetStartupUriAsync(string startupUri);

    Task<string?> GetShutdownModeAsync();
    Task SetShutdownModeAsync(string shutdownMode);
}
