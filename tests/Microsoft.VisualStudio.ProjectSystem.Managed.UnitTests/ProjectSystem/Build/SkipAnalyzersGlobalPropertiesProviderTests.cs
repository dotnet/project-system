// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Managed.Build;

namespace Microsoft.VisualStudio.ProjectSystem.Build
{
    public sealed class SkipAnalyzersGlobalPropertiesProviderTests
    {
        [Theory, CombinatorialData]
        public async Task TestSkipAnalyzersGlobalPropertiesProvider(
            bool implicitBuild,
            bool skipAnalyzersSettingTurnedOn)
        {
            UnconfiguredProject project = UnconfiguredProjectFactory.Create(
                unconfiguredProjectServices: UnconfiguredProjectServicesFactory.Create(
                    projectService: IProjectServiceFactory.Create()));
            IImplicitlyTriggeredBuildState buildState = IImplicityTriggeredBuildStateFactory.Create(implicitBuild);
            IProjectSystemOptions options = IProjectSystemOptionsFactory.ImplementGetSkipAnalyzersForImplicitlyTriggeredBuildAsync(ct => skipAnalyzersSettingTurnedOn);

            SkipAnalyzersGlobalPropertiesProvider provider = new SkipAnalyzersGlobalPropertiesProvider(
                project,
                buildState,
                options);

            IImmutableDictionary<string, string> properties = await provider.GetGlobalPropertiesAsync(CancellationToken.None);

            if (implicitBuild && skipAnalyzersSettingTurnedOn)
            {
                Assert.Equal(expected: 2, actual: properties.Count);
                Assert.Equal(expected: "true", actual: properties["IsImplicitlyTriggeredBuild"]);
                Assert.Equal(expected: "ImplicitBuild", actual: properties["FastUpToDateCheckIgnoresKinds"]);
            }
            else
            {
                Assert.Empty(properties);
            }
        }
    }
}
