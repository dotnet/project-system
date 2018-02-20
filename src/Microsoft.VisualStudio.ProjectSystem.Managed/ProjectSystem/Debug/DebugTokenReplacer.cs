// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public DebugTokenReplacer(IUnconfiguredProjectCommonServices unconnfiguredServices, IEnvironmentHelper environmentHelper,
                                  IActiveDebugFrameworkServices activeDebugFrameworkService, IProjectAccessor projectAccessor)
        {
            UnconfiguredServices = unconnfiguredServices;
            EnvironmentHelper = environmentHelper;
            ActiveDebugFrameworkService = activeDebugFrameworkService;
            ProjectAccessor = projectAccessor;
        }

        private IUnconfiguredProjectCommonServices UnconfiguredServices { get; }
        private IEnvironmentHelper EnvironmentHelper { get; }
        private IActiveDebugFrameworkServices ActiveDebugFrameworkService { get; }
        private IProjectAccessor ProjectAccessor { get; }

        // Regular expression string to extract $(sometoken) elements from a string
        private static Regex s_matchTokenRegex = new Regex(@"(\$\((?<token>[^\)]+)\))", RegexOptions.IgnoreCase);

        /// <summary>
        /// Walks the profile and returns a new one where all the tokens have been replaced. Tokens can consist of 
        /// environment variables (%var%), or any msbuild property $(msbuildproperty). Environment variables are 
        /// replaced first, followed by msbuild properties.
        /// </summary>
        public async Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile)
        {
            var resolvedProfile = new LaunchProfile(profile);
            if (!string.IsNullOrWhiteSpace(resolvedProfile.ExecutablePath))
            {
                resolvedProfile.ExecutablePath = await ReplaceTokensInStringAsync(resolvedProfile.ExecutablePath, true).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(resolvedProfile.CommandLineArgs))
            {
                resolvedProfile.CommandLineArgs = await ReplaceTokensInStringAsync(resolvedProfile.CommandLineArgs, true).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(resolvedProfile.WorkingDirectory))
            {
                resolvedProfile.WorkingDirectory = await ReplaceTokensInStringAsync(resolvedProfile.WorkingDirectory, true).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(resolvedProfile.LaunchUrl))
            {
                resolvedProfile.LaunchUrl = await ReplaceTokensInStringAsync(resolvedProfile.LaunchUrl, true).ConfigureAwait(false);
            }

            // Since Env variables are an immutable dictionary they are a little messy to update.
            if (resolvedProfile.EnvironmentVariables != null)
            {
                foreach (var kvp in resolvedProfile.EnvironmentVariables)
                {
                    resolvedProfile.EnvironmentVariables = resolvedProfile.EnvironmentVariables.SetItem(kvp.Key, await ReplaceTokensInStringAsync(kvp.Value, true).ConfigureAwait(false));
                }
            }

            if (resolvedProfile.OtherSettings != null)
            {
                foreach (var kvp in resolvedProfile.OtherSettings)
                {
                    if (kvp.Value is string)
                    {
                        resolvedProfile.OtherSettings = resolvedProfile.OtherSettings.SetItem(kvp.Key, await ReplaceTokensInStringAsync((string)kvp.Value, true).ConfigureAwait(false));
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

            string expandedString = expandEnvironmentVars? EnvironmentHelper.ExpandEnvironmentVariables(rawString) : rawString;

            return ReplaceMSBuildTokensInStringAsync(expandedString);
        }

        private async Task<string> ReplaceMSBuildTokensInStringAsync(string rawString)
        {
            MatchCollection matches = s_matchTokenRegex.Matches(rawString);
            if (matches.Count == 0)
                return rawString;

            ConfiguredProject configuredProject = await ActiveDebugFrameworkService.GetConfiguredProjectForActiveFrameworkAsync()
                                                                                   .ConfigureAwait(true);

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

            }).ConfigureAwait(true);
        }
    }
}
