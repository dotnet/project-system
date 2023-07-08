// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
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

        public async Task<ILaunchProfile> ReplaceTokensInProfileAsync(ILaunchProfile profile)
        {
            return await LaunchProfile.ReplaceTokensAsync(
                profile,
                str => ReplaceTokensInStringAsync(str, expandEnvironmentVars: true));
        }

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
