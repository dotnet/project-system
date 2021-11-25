// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Given an ILaunchProfile, it will enumerate the items and do replacement on the each string
    /// entry.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IDebugTokenReplacer
    {
        Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile);

        Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars);
    }
}
