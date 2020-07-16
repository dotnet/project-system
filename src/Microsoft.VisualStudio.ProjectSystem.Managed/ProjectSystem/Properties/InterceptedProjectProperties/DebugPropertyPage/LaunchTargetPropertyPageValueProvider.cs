// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
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
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ConfiguredProject configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            IPropertyPagesCatalogProvider catalogProvider = configuredProject?.Services.PropertyPagesCatalog;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (catalogProvider == null)
            {
                return null;
            }

            IPropertyPagesCatalog catalog = await catalogProvider.GetCatalogAsync(PropertyPageContexts.Project);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Microsoft.Build.Framework.XamlTypes.Rule rule = catalog.GetSchema(unevaluatedPropertyValue);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (rule == null)
            {
                return null;
            }

            if (rule.Metadata.TryGetValue("CommandName", out object pageCommandNameObj)
                && pageCommandNameObj is string pageCommandName)
            {
                _projectThreadingService.RunAndForget(async () =>
                {
                    ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);

                    IWritableLaunchSettings writableLaunchSettings = launchSettings.ToWritableLaunchSettings();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    IWritableLaunchProfile activeProfile = writableLaunchSettings.ActiveProfile;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
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
            ILaunchSettings launchSettings = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string commandName = launchSettings.ActiveProfile?.CommandName;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (commandName == null)
            {
                return string.Empty;
            }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            ConfiguredProject configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            IPropertyPagesCatalogProvider catalogProvider = configuredProject?.Services.PropertyPagesCatalog;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (catalogProvider == null)
            {
                return string.Empty;
            }

            IPropertyPagesCatalog catalog = await catalogProvider.GetCatalogAsync(PropertyPageContexts.Project);
            foreach (string schemaName in catalog.GetPropertyPagesSchemas())
            {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                Microsoft.Build.Framework.XamlTypes.Rule rule = catalog.GetSchema(schemaName);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
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
