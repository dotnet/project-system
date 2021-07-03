// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// The complex Target Framework property consists of three UI-exposed properties: target framework moniker, target platform and target platform version.
    /// Those combine and produce a single value like this: [target framework alias]-[target platform][target platform version].
    /// This class intercepts those properties from the UI to saved their values into the TargetFramework property.
    /// In the case of retrieving the values of those properties, this class looks for the associated properties found in the project's configuration.
    /// </summary>
    [ExportInterceptingPropertyValueProvider(
        new[]
        {
            InterceptedTargetFrameworkProperty,
            TargetPlatformProperty,
            TargetPlatformVersionProperty
        },
        ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class ComplexTargetFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string InterceptedTargetFrameworkProperty = "InterceptedTargetFramework";
        private const string TargetPlatformProperty = ConfigurationGeneral.TargetPlatformIdentifierProperty;
        private const string TargetPlatformVersionProperty = ConfigurationGeneral.TargetPlatformVersionProperty;
        private const string TargetFrameworkProperty = ConfigurationGeneral.TargetFrameworkProperty;
        private const string MinimumPlatformVersionProperty = "MinimumPlatformVersion";

        private readonly ProjectProperties _properties;
        private readonly ConfiguredProject _configuredProject;

        private struct ComplexTargetFramework
        {
            public string? TargetFrameworkMoniker;
            public string? TargetPlatformIdentifier;
            public string? TargetPlatformVersion;
            public string? TargetFramework;
        }

        [ImportingConstructor]
        public ComplexTargetFrameworkValueProvider(ProjectProperties properties, ConfiguredProject configuredProject)
        {
            _properties = properties;
            _configuredProject = configuredProject;
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            ComplexTargetFramework storedValues = await GetStoredComplexTargetFrameworkAsync(configuration);

            if (storedValues.TargetFrameworkMoniker != null)
            {
                if (StringComparers.PropertyLiteralValues.Equals(propertyName, InterceptedTargetFrameworkProperty))
                {
                    storedValues.TargetFrameworkMoniker = unevaluatedPropertyValue;
                }
                else if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetPlatformProperty))
                {
                    if (unevaluatedPropertyValue != storedValues.TargetPlatformIdentifier)
                    {
                        storedValues.TargetPlatformIdentifier = unevaluatedPropertyValue;
                        await ResetPlatformPropertiesAsync(defaultProperties);
                    }
                }
                else if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetPlatformVersionProperty))
                {
                    storedValues.TargetPlatformVersion = unevaluatedPropertyValue;
                }
                await defaultProperties.SetPropertyValueAsync(TargetFrameworkProperty, await ComputeValueAsync(storedValues));
            }

            return null;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? storedValue = await GetStoredValueAsync(configuration, propertyName);

            if (storedValue is null)
            {
                return string.Empty;
            }

            return await base.OnGetEvaluatedPropertyValueAsync(propertyName, storedValue, defaultProperties);
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? storedValue = await GetStoredValueAsync(configuration, propertyName);

            if (storedValue is null)
            {
                return string.Empty;
            }

            return await base.OnGetUnevaluatedPropertyValueAsync(propertyName, storedValue, defaultProperties);
        }

        private static async Task<ComplexTargetFramework> GetStoredComplexTargetFrameworkAsync(ConfigurationGeneral configuration)
        {
            ComplexTargetFramework storedValues = new ComplexTargetFramework
            {
                TargetFrameworkMoniker = (string?)await configuration.TargetFrameworkMoniker.GetValueAsync(),
                TargetPlatformIdentifier = (string?)await configuration.TargetPlatformIdentifier.GetValueAsync(),
                TargetPlatformVersion = (string?)await configuration.TargetPlatformVersion.GetValueAsync(),
                TargetFramework = (string?)await configuration.TargetFramework.GetValueAsync()
            };

            return storedValues;
        }

        private static async Task<string?> GetStoredValueAsync(ConfigurationGeneral configuration, string propertyName)
        {
            return propertyName switch
            {
                InterceptedTargetFrameworkProperty => (string?)await configuration.TargetFrameworkMoniker.GetValueAsync(),
                TargetPlatformProperty => (string?)await configuration.TargetPlatformIdentifier.GetValueAsync(),
                TargetPlatformVersionProperty => (string?)await configuration.TargetPlatformVersion.GetValueAsync(),
                TargetFrameworkProperty => (string?)await configuration.TargetFramework.GetValueAsync(),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Combines the three values that make up the TargetFramework property.
        /// I.e. { net5.0, windows, 10.0 } => net5.0-windows10.0
        /// { net5.0, null, null } => net5.0
        /// { null, null, null } => String.Empty
        /// </summary>
        /// <param name="complexTargetFramework">A struct with each property.</param>
        /// <returns>A string with the combined values.</returns>
        private async Task<string> ComputeValueAsync(ComplexTargetFramework complexTargetFramework)
        {
            if (Strings.IsNullOrEmpty(complexTargetFramework.TargetFrameworkMoniker))
            {
                return string.Empty;
            }

            string targetFrameworkAlias = await GetTargetFrameworkAliasAsync(complexTargetFramework.TargetFrameworkMoniker);

            if (string.IsNullOrEmpty(targetFrameworkAlias))
            {
                // The value on the TargetFrameworkMoniker is not on the supported list and we shouldn't try to parse it.
                // Therefore, we return the user value as it is. I.e. <TargetFramework>foo</TargetFramework>
                if (!Strings.IsNullOrEmpty(complexTargetFramework.TargetFramework))
                {
                    return complexTargetFramework.TargetFramework;
                }
            }

            if (!string.IsNullOrEmpty(complexTargetFramework.TargetPlatformIdentifier) && complexTargetFramework.TargetPlatformIdentifier != null)
            {
                string targetPlatformAlias = await GetTargetPlatformAliasAsync(complexTargetFramework.TargetPlatformIdentifier);

                if (string.IsNullOrEmpty(targetPlatformAlias))
                {
                    return targetFrameworkAlias;
                }

                return targetFrameworkAlias + "-" + targetPlatformAlias + complexTargetFramework.TargetPlatformVersion;
            }

            return targetFrameworkAlias;
        }

        /// <summary>
        /// Retrieves the target framework alias (i.e. net5.0) from the project's subscription service.
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetTargetFrameworkAliasAsync(string targetFrameworkMoniker)
        {
            IProjectSubscriptionService? subscriptionService = _configuredProject.Services.ProjectSubscription;
            Assumes.Present(subscriptionService);

            IImmutableDictionary<string, IProjectRuleSnapshot> supportedTargetFrameworks = await subscriptionService.ProjectRuleSource.GetLatestVersionAsync(_configuredProject, new string[] { SupportedTargetFramework.SchemaName });
            IProjectRuleSnapshot targetFrameworkRuleSnapshot = supportedTargetFrameworks[SupportedTargetFramework.SchemaName];

            IImmutableDictionary<string, string>? targetFrameworkProperties = targetFrameworkRuleSnapshot.GetProjectItemProperties(targetFrameworkMoniker);

            if (targetFrameworkProperties != null &&
                targetFrameworkProperties.TryGetValue(SupportedTargetFramework.AliasProperty, out string? targetFrameworkAlias))
            {
                return targetFrameworkAlias;
            }

            return string.Empty;
        }

        /// <summary>
        /// Retrieves the associated platform alias (i.e. windows) from the project's subscription service.
        /// </summary>
        /// <param name="targetPlatformIdentifier"></param>
        /// <returns></returns>
        private async Task<string> GetTargetPlatformAliasAsync(string targetPlatformIdentifier)
        {
            IProjectSubscriptionService? subscriptionService = _configuredProject.Services.ProjectSubscription;
            Assumes.Present(subscriptionService);

            IImmutableDictionary<string, IProjectRuleSnapshot> sdkSupportedTargetPlatformIdentifiers = await subscriptionService.ProjectRuleSource.GetLatestVersionAsync(_configuredProject, new string[] { SdkSupportedTargetPlatformIdentifier.SchemaName });

            IProjectRuleSnapshot sdkSupportedTargetPlatformIdentifierRuleSnapshot = sdkSupportedTargetPlatformIdentifiers[SdkSupportedTargetPlatformIdentifier.SchemaName];

            // The SdkSupportedTargetPlatformIdentifier rule stores the alias in the key value.
            if (sdkSupportedTargetPlatformIdentifierRuleSnapshot.Items.TryGetKey(targetPlatformIdentifier, out string targetPlatformAlias))
            {
                return targetPlatformAlias;
            }

            return string.Empty;
        }

        /// <summary>
        /// Resets the values on the TargetPlatformProperty and MinimumPlatformVersionProperty.
        /// </summary>
        /// <param name="projectProperties"></param>
        /// <returns></returns>
        private static async Task ResetPlatformPropertiesAsync(IProjectProperties projectProperties)
        {
            await projectProperties.DeletePropertyAsync(TargetPlatformVersionProperty);
            await projectProperties.DeletePropertyAsync(MinimumPlatformVersionProperty);
        }
    }
}
