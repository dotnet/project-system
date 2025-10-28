// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

[Export(typeof(IDebugTokenReplacer))]
[AppliesTo(ProjectCapability.LaunchProfiles)]
internal sealed class DebugTokenReplacer : IDebugTokenReplacer
{
    // Regular expression string to extract $(sometoken) elements from a string
    private static readonly Regex s_matchTokenRegex = new(@"\$\((?<token>[^\)]+)\)", RegexOptions.IgnoreCase);

    private readonly IEnvironment _environment;
    private readonly IActiveDebugFrameworkServices _activeDebugFrameworkService;
    private readonly IProjectAccessor _projectAccessor;

    [ImportingConstructor]
    public DebugTokenReplacer(IEnvironment environment, IActiveDebugFrameworkServices activeDebugFrameworkService, IProjectAccessor projectAccessor)
    {
        _environment = environment;
        _activeDebugFrameworkService = activeDebugFrameworkService;
        _projectAccessor = projectAccessor;
    }

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
            ? _environment.ExpandEnvironmentVariables(rawString)
            : rawString;

        if (!s_matchTokenRegex.IsMatch(expandedString))
            return Task.FromResult(expandedString);

        return ReplaceMSBuildTokensAsync();

        async Task<string> ReplaceMSBuildTokensAsync()
        {
            ConfiguredProject? configuredProject = await _activeDebugFrameworkService.GetConfiguredProjectForActiveFrameworkAsync();

            Assumes.NotNull(configuredProject);

            return await _projectAccessor.OpenProjectForReadAsync(
                configuredProject,
                project => s_matchTokenRegex.Replace(expandedString, m => project.ExpandString(m.Value)));
        }
    }
}
