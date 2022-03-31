// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    /// <summary>
    ///     Turns off UseHostCompilerIfAvailable to prevent CoreCompile from always being called during builds, see: https://github.com/dotnet/sdk/issues/708.
    /// </summary>
    [ExportBuildGlobalPropertiesProvider]
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class TurnOffUseHostCompilerIfAvailableBuildPropertiesProvider : StaticGlobalPropertiesProviderBase
    {
        private static readonly Task<IImmutableDictionary<string, string>> s_buildProperties = Task.FromResult<IImmutableDictionary<string, string>>(
            Empty.PropertiesMap.Add(BuildProperty.UseHostCompilerIfAvailable, "false"));

        [ImportingConstructor]
        public TurnOffUseHostCompilerIfAvailableBuildPropertiesProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            return s_buildProperties;
        }
    }
}
