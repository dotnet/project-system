// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private static readonly Regex s_matchTokenRegex = new(@"(\$\((?<token>[^\)]+)\))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Walks the profile and returns a new one where all the tokens have been replaced. Tokens can consist of
        /// environment variables (%var%), or any msbuild property $(msbuildproperty). Environment variables are
        /// replaced first, followed by msbuild properties.
        /// </summary>
        public async Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile)
        {
            var resolvedProfile = new LaunchProfile(profile);
            if (!Strings.IsNullOrWhiteSpace(resolvedProfile.ExecutablePath))
            {
                resolvedProfile.ExecutablePath = await ReplaceTokensInStringAsync(resolvedProfile.ExecutablePath, true);
            }

            if (!Strings.IsNullOrWhiteSpace(resolvedProfile.CommandLineArgs))
            {
                resolvedProfile.CommandLineArgs = await ReplaceTokensInStringAsync(resolvedProfile.CommandLineArgs, true);
            }

            if (!Strings.IsNullOrWhiteSpace(resolvedProfile.WorkingDirectory))
            {
                resolvedProfile.WorkingDirectory = await ReplaceTokensInStringAsync(resolvedProfile.WorkingDirectory, true);
            }

            if (!Strings.IsNullOrWhiteSpace(resolvedProfile.LaunchUrl))
            {
                resolvedProfile.LaunchUrl = await ReplaceTokensInStringAsync(resolvedProfile.LaunchUrl, true);
            }

            // Since Env variables are an immutable dictionary they are a little messy to update.
            if (resolvedProfile.EnvironmentVariables != null)
            {
                foreach ((string key, string value) in resolvedProfile.EnvironmentVariables)
                {
                    resolvedProfile.EnvironmentVariables = resolvedProfile.EnvironmentVariables.SetItem(key, await ReplaceTokensInStringAsync(value, true));
                }
            }

            if (resolvedProfile.OtherSettings != null)
            {
                foreach ((string key, object value) in resolvedProfile.OtherSettings)
                {
                    if (value is string s)
                    {
                        resolvedProfile.OtherSettings = resolvedProfile.OtherSettings.SetItem(key, await ReplaceTokensInStringAsync(s, true));
                    }
                }
            }

            return resolvedProfile;
        }

        /// <summary>
        /// Replaces the tokens and environment variables in a single string. If expandEnvironmentVars
        /// is true, they are expanded first before replacement happens. If the rawString is null or empty
        /// it is returned as is.
        /// </summary>
        public Task<string> ReplaceTokensInStringAsync(string rawString, bool expandEnvironmentVars)
        {
            if (string.IsNullOrWhiteSpace(rawString))
            {
                return Task.FromResult(rawString);
            }

            string expandedString = expandEnvironmentVars
                ? EnvironmentHelper.ExpandEnvironmentVariables(rawString)
                : rawString;

            return ReplaceMSBuildTokensInStringAsync(expandedString);
        }

        private async Task<string> ReplaceMSBuildTokensInStringAsync(string rawString)
        {
            MatchCollection matches = s_matchTokenRegex.Matches(rawString);
            if (matches.Count == 0)
                return rawString;

            ConfiguredProject? configuredProject = await ActiveDebugFrameworkService.GetConfiguredProjectForActiveFrameworkAsync();

            Assumes.NotNull(configuredProject);

            return await ProjectAccessor.OpenProjectForReadAsync(configuredProject, project =>
            {
                string expandedString = rawString;

                // For each token we try to get a replacement value.
                foreach (Match match in matches)
                {
                    // Resolve with msbuild. It will return the empty string if not found
                    expandedString = expandedString.Replace(match.Value, project.ExpandString(match.Value));
                }

                return expandedString;
            });
        }
    }
}
