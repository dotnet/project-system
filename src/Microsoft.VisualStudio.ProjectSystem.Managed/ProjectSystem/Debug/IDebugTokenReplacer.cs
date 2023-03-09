// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Replaces tokens of form <c>%VAR%</c> with their environment variable value, and of form
    /// <c>$(Property)</c> with the active configured project's MSBuild evaluation values.
    /// </summary>
    /// <remarks>
    /// Supports token substitution in <see cref="ILaunchProfile"/> data via <see cref="ReplaceTokensInProfileAsync"/>,
    /// but can also substitute individual strings via <see cref="ReplaceTokensInStringAsync"/>.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IDebugTokenReplacer
    {
        /// <summary>
        /// Walks the profile and returns a new one where all the tokens have been replaced. Tokens can consist of
        /// environment variables (<c>%VAR%</c>), or any MSBuild property (<c>$(Property)</c>).
        /// </summary>
        /// <remarks>
        /// Environment variables are replaced first, followed by MSBuild properties.
        /// </remarks>
        Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile);

        /// <summary>
        /// Replaces the tokens and environment variables in a single string. If expandEnvironmentVars
        /// is true, they are expanded first before replacement happens.
        /// </summary>
        /// <remarks>
        /// If <paramref name="rawString"/> is <see langword="null"/> or empty, it is returned as is.
        /// </remarks>
        Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars);
    }
}
