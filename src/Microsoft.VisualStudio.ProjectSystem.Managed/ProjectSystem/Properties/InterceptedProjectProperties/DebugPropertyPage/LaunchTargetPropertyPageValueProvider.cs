// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider(LaunchTargetPropertyPagePropertyName, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal class LaunchTargetPropertyPageValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string LaunchTargetPropertyPagePropertyName = "LaunchTargetPropertyPage";

        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly IProjectThreadingService _projectThreadingService;

        [ImportingConstructor]
        public LaunchTargetPropertyPageValueProvider(
            UnconfiguredProject project,
            ILaunchSettingsProvider launchSettingsProvider,
            IProjectThreadingService projectThreadingService)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _projectThreadingService = projectThreadingService;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            return GetPropertyValueAsync();
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            ConfiguredProject? configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
            IPropertyPagesCatalogProvider? catalogProvider = configuredProject?.Services.PropertyPagesCatalog;
            if (catalogProvider == null)
            {
                return null;
            }

            IPropertyPagesCatalog catalog = await catalogProvider.GetCatalogAsync(PropertyPageContexts.Project);
            Rule? rule = catalog.GetSchema(unevaluatedPropertyValue);
            if (rule == null)
            {
                return null;
            }

            if (rule.Metadata.TryGetValue("CommandName", out object pageCommandNameObj)
                && pageCommandNameObj is string pageCommandName)
            {
                _projectThreadingService.RunAndForget(async () =>
                {
                    // Infinite timeout means this will not actually be null.
                    ILaunchSettings? launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
                    Assumes.NotNull(launchSettings);

                    IWritableLaunchSettings writableLaunchSettings = launchSettings.ToWritableLaunchSettings();
                    IWritableLaunchProfile? activeProfile = writableLaunchSettings.ActiveProfile;
                    if (activeProfile != null)
                    {
                        activeProfile.CommandName = pageCommandName;

                        await _launchSettingsProvider.UpdateAndSaveSettingsAsync(writableLaunchSettings.ToLaunchSettings());
                    }
                },
                options: ForkOptions.HideLocks,
                unconfiguredProject: _project);
            }

            return null;
        }

        private async Task<string> GetPropertyValueAsync()
        {
            // Infinite timeout means this will not actually be null.
            ILaunchSettings? launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
            Assumes.NotNull(launchSettings);

            string? commandName = launchSettings.ActiveProfile?.CommandName;
            if (commandName == null)
            {
                return string.Empty;
            }

            ConfiguredProject? configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
            IPropertyPagesCatalogProvider? catalogProvider = configuredProject?.Services.PropertyPagesCatalog;

            if (catalogProvider == null)
            {
                return string.Empty;
            }

            IPropertyPagesCatalog catalog = await catalogProvider.GetCatalogAsync(PropertyPageContexts.Project);
            foreach (string schemaName in catalog.GetPropertyPagesSchemas())
            {
                Rule? rule = catalog.GetSchema(schemaName);
                if (rule != null
                    && string.Equals(rule.PageTemplate, "CommandNameBasedDebugger", StringComparison.OrdinalIgnoreCase)
                    && rule.Metadata.TryGetValue("CommandName", out object pageCommandNameObj)
                    && pageCommandNameObj is string pageCommandName
                    && pageCommandName.Equals(commandName))
                {
                    return schemaName;
                }
            }

            return string.Empty;
        }
    }
}
