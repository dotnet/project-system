// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Token replacer can be imported to replace all the msbuild property and environment variable tokens in an ILaunchProfile or
    /// in an individual string
    /// </summary>
    [Export(typeof(IDebugTokenReplacer))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class DebugTokenReplacer : IDebugTokenReplacer
    {
        [ImportingConstructor]
        public DebugTokenReplacer(IEnvironmentHelper environmentHelper, IActiveDebugFrameworkServices activeDebugFrameworkService, IProjectAccessor projectAccessor)
        {
            EnvironmentHelper = environmentHelper;
            ActiveDebugFrameworkService = activeDebugFrameworkService;
            ProjectAccessor = projectAccessor;
        }

        private IEnvironmentHelper EnvironmentHelper { get; }
        private IActiveDebugFrameworkServices ActiveDebugFrameworkService { get; }
        private IProjectAccessor ProjectAccessor { get; }

        // Regular expression string to extract $(sometoken) elements from a string
        private static readonly Regex s_matchTokenRegex = new(@"\$\((?<token>[^\)]+)\)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Walks the profile and returns a new one where all the tokens have been replaced. Tokens can consist of
        /// environment variables (%var%), or any msbuild property $(msbuildproperty). Environment variables are
        /// replaced first, followed by msbuild properties.
        /// </summary>
        public async Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile)
        {
            return await LaunchProfile.ReplaceTokensAsync(
                profile,
                str => ReplaceTokensInStringAsync(str, expandEnvironmentVars: true));
        }

        /// <summary>
        /// Replaces the tokens and environment variables in a single string. If expandEnvironmentVars
        /// is true, they are expanded first before replacement happens. If the rawString is null or empty
        /// it is returned as is.
        /// </summary>
        public Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars)
        {
            if (string.IsNullOrWhiteSpace(rawString))
                return Task.FromResult(rawString);

            string expandedString = expandEnvironmentVars
                ? EnvironmentHelper.ExpandEnvironmentVariables(rawString)
                : rawString;

            if (!s_matchTokenRegex.IsMatch(expandedString))
                return Task.FromResult(expandedString);

            return ReplaceMSBuildTokensAsync();

            async Task<string> ReplaceMSBuildTokensAsync()
            {
                ConfiguredProject? configuredProject = await ActiveDebugFrameworkService.GetConfiguredProjectForActiveFrameworkAsync();

                Assumes.NotNull(configuredProject);

                return await ProjectAccessor.OpenProjectForReadAsync(
                    configuredProject,
                    project => s_matchTokenRegex.Replace(expandedString, m => project.ExpandString(m.Value)));
            }
        }
    }
}
