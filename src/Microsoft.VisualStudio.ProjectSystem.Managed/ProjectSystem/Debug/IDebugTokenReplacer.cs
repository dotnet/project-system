// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Replaces tokens of form <c>%VAR%</c> with their environment variable value, and of form
    /// <c>$(MSBuildExpression)</c> with the evaluated expression via the active configured project.
    /// </summary>
    /// <remarks>
    /// Intended for token substitution in <see cref="ILaunchProfile"/> data. Custom launch profile
    /// providers may import this component to perform such substitutions.
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    public interface IDebugTokenReplacer
    {
        /// <summary>
        /// Returns a copy of <paramref name="profile"/> where all tokens have been replaced.
        /// Tokens can consist of environment variables (<c>%VAR%</c>), or MSBuild expressions (<c>$(Property)</c>).
        /// </summary>
        /// <remarks>
        /// Environment variables are replaced first, followed by MSBuild properties.
        /// </remarks>
        Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile);

        /// <summary>
        /// Replaces the MSBuild expressions and (optionally) environment variables in <paramref name="rawString"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <paramref name="expandEnvironmentVars"/> is <see langword="true"/>, environment variables are substituted. Environment variables are substituted before MSBuild expressions.
        /// </para>
        /// <para>
        /// If <paramref name="rawString"/> is <see langword="null"/> or empty, it is returned as is.
        /// </para>
        /// </remarks>
        Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars);
    }
}
