// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    public class LaunchTargetPropertyPageValueProviderTests
    {
        [Fact]
        public async Task GetProperty_ReturnsEmptyString_WhenActiveLaunchProfileHasNoCommand()
        {
            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(),
                launchSettingsProvider: SetupLaunchSettingsProvider(activeProfileName: "Alpha"),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: string.Empty, actual: actualValue);
        }

        [Fact]
        public async Task GetProperty_ReturnsEmptyString_WhenThereIsNoPropertyPagesCatalog()
        {
            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: null))),
                launchSettingsProvider: SetupLaunchSettingsProvider(activeProfileName: "Alpha", activeProfileLaunchTarget: "AlphaCommand"),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: string.Empty, actual: actualValue);
        }

        [Fact]
        public async Task GetProperty_ReturnsEmptyString_WhenNoPageHasAMatchingCommandName()
        {
            var catalogProvider = GetCatalogProviderAndData();

            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: catalogProvider))),
                launchSettingsProvider: SetupLaunchSettingsProvider(activeProfileName: "Alpha", activeProfileLaunchTarget: "AlphaCommand"),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: string.Empty, actual: actualValue);
        }

        [Fact]
        public async Task GetProperty_ReturnsPageName_WhenAPageHasAMatchingCommandName()
        {
            var catalogProvider = GetCatalogProviderAndData();

            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: catalogProvider))),
                launchSettingsProvider: SetupLaunchSettingsProvider(activeProfileName: "Alpha", activeProfileLaunchTarget: "BetaCommand"),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: "BetaPage", actual: actualValue);
        }

        [Fact]
        public async Task GetProperty_DoesNotFail_WhenAPageHasNoTemplate()
        {
            var catalogProvider = GetCatalogAndProviderDataWithMissingTemplateName();

            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: catalogProvider))),
                launchSettingsProvider: SetupLaunchSettingsProvider(activeProfileName: "Alpha", activeProfileLaunchTarget: "BetaCommand"),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnGetEvaluatedPropertyValueAsync(string.Empty, string.Empty, Mock.Of<IProjectProperties>());

            Assert.Equal(expected: string.Empty, actual: actualValue);
        }

        [Fact]
        public async Task SetProperty_DoesNothing_WhenThereIsNoPropertyPagesCatalog()
        {
            bool launchSettingsUpdated = false;

            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: null))),
                launchSettingsProvider: SetupLaunchSettingsProvider(
                    activeProfileName: "Alpha",
                    activeProfileLaunchTarget: "AlphaCommand",
                    updateLaunchSettingsCallback: ls => launchSettingsUpdated = true),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnSetPropertyValueAsync(string.Empty, "BetaPage", Mock.Of<IProjectProperties>());

            Assert.Null(actualValue);
            Assert.False(launchSettingsUpdated);
        }

        [Fact]
        public async Task SetProperty_DoesNothing_WhenSpecifiedPropertyPageIsNotFound()
        {
            bool launchSettingsUpdated = false;

            var catalogProvider = GetCatalogProviderAndData();

            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: catalogProvider))),
                launchSettingsProvider: SetupLaunchSettingsProvider(
                    activeProfileName: "Alpha",
                    activeProfileLaunchTarget: "AlphaCommand",
                    updateLaunchSettingsCallback: ls => launchSettingsUpdated = true),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnSetPropertyValueAsync(string.Empty, "EpsilonPage", Mock.Of<IProjectProperties>());

            Assert.Null(actualValue);
            Assert.False(launchSettingsUpdated);
        }

        [Fact]
        public async Task  SetProperty_UpdatesActiveProfileCommand_WhenAPageWithAMatchingNameIsFound()
        {
            string? newLaunchTarget = string.Empty;

            var catalogProvider = GetCatalogProviderAndData();

            var provider = new LaunchTargetPropertyPageValueProvider(
                project: UnconfiguredProjectFactory.Create(
                    configuredProject: ConfiguredProjectFactory.Create(
                        services: ConfiguredProjectServicesFactory.Create(
                            propertyPagesCatalogProvider: catalogProvider))),
                launchSettingsProvider: SetupLaunchSettingsProvider(
                    activeProfileName: "Alpha",
                    activeProfileLaunchTarget: "AlphaCommand",
                    updateLaunchSettingsCallback: ls => newLaunchTarget = ls.ActiveProfile!.CommandName),
                projectThreadingService: IProjectThreadingServiceFactory.Create());

            var actualValue = await provider.OnSetPropertyValueAsync(string.Empty, "GammaPage", Mock.Of<IProjectProperties>());

            Assert.Null(actualValue);
            Assert.Equal(expected: "GammaCommand", actual: newLaunchTarget);
        }

        private static ILaunchSettingsProvider SetupLaunchSettingsProvider(
            string activeProfileName,
            string? activeProfileLaunchTarget = null,
            Action<ILaunchSettings>? updateLaunchSettingsCallback = null)
        {
            var profile = new WritableLaunchProfile
            {
                Name = activeProfileName
            };

            if (activeProfileLaunchTarget != null)
            {
                profile.CommandName = activeProfileLaunchTarget;
            }

            var settingsProvider = ILaunchSettingsProviderFactory.Create(
                activeProfileName,
                new[] { profile.ToLaunchProfile() },
                updateLaunchSettingsCallback: updateLaunchSettingsCallback);

            return settingsProvider;
        }

        private static IPropertyPagesCatalogProvider GetCatalogProviderAndData()
        {
            var betaPage = ProjectSystem.IRuleFactory.Create(
                pageTemplate: "CommandNameBasedDebugger",
                metadata: new Dictionary<string, object>
                {
                    { "CommandName", "BetaCommand" }
                });
            var gammaPage = ProjectSystem.IRuleFactory.Create(
                pageTemplate: "CommandNamedBasedDebugger",
                metadata: new Dictionary<string, object>
                {
                    { "CommandName", "GammaCommand" }
                });

            var catalog = IPropertyPagesCatalogFactory.Create(
                new Dictionary<string, IRule>
                {
                    { "BetaPage", betaPage },
                    { "GammaPage", gammaPage }
                });

            var catalogProvider = IPropertyPagesCatalogProviderFactory.Create(
                new Dictionary<string, IPropertyPagesCatalog> { { "Project", catalog } });

            return catalogProvider;
        }

        private static IPropertyPagesCatalogProvider GetCatalogAndProviderDataWithMissingTemplateName()
        {
            var pageWithNoTemplate = ProjectSystem.IRuleFactory.Create(
                pageTemplate: null,
                metadata: new Dictionary<string, object>
                {
                    { "CommandName", "BetaCommand" }
                });

            var catalog = IPropertyPagesCatalogFactory.Create(
                new Dictionary<string, IRule>
                {
                    { "PageWithNoTemplate", pageWithNoTemplate },
                });

            var catalogProvider = IPropertyPagesCatalogProviderFactory.Create(
                new Dictionary<string, IPropertyPagesCatalog> { { "Project", catalog } });

            return catalogProvider;
        }
    }
}
