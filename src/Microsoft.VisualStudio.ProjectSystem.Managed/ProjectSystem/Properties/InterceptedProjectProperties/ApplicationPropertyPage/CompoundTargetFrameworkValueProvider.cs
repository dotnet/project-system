// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

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
    internal class CompoundTargetFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string InterceptedTargetFrameworkProperty = "InterceptedTargetFramework";
        private const string TargetPlatformProperty = ConfigurationGeneral.TargetPlatformIdentifierProperty;
        private const string TargetPlatformVersionProperty = ConfigurationGeneral.TargetPlatformVersionProperty;
        private const string TargetFrameworkProperty = ConfigurationGeneral.TargetFrameworkProperty;
        private const string SupportedOSPlatformVersionProperty = "SupportedOSPlatformVersion";

        private readonly ProjectProperties _properties;
        private readonly ConfiguredProject _configuredProject;
        private bool? _useWPFProperty;
        private bool? _useWindowsFormsProperty;
        private bool? _useWinUIProperty;
        private static readonly string[] s_msBuildPropertyNames = { TargetFrameworkProperty, TargetPlatformProperty, TargetPlatformVersionProperty, SupportedOSPlatformVersionProperty };
        private static readonly Regex s_versionRegex = new(@"^net(?<version>[0-9.]+)$", RegexOptions.ExplicitCapture);

        private struct ComplexTargetFramework
        {
            public string? TargetFrameworkMoniker;
            public string? TargetPlatformIdentifier;
            public string? TargetPlatformVersion;
            public string? TargetFrameworkIdentifier;
            public string? TargetFramework;
        }

        [ImportingConstructor]
        public CompoundTargetFrameworkValueProvider(ProjectProperties properties)
        {
            _properties = properties;
            _configuredProject = properties.ConfiguredProject;
        }

        private async Task<ComplexTargetFramework> GetStoredPropertiesAsync()
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            return await GetStoredComplexTargetFrameworkAsync(configuration);
        }

        public override Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties)
        {
            return IsValueDefinedInContextMSBuildPropertiesAsync(defaultProperties, s_msBuildPropertyNames);
        }

        public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            ComplexTargetFramework storedProperties = await GetStoredPropertiesAsync();

            if (storedProperties.TargetFrameworkMoniker is not null)
            {
                // Changing the Target Framework Moniker
                if (StringComparers.PropertyLiteralValues.Equals(propertyName, InterceptedTargetFrameworkProperty))
                {
                    if (Strings.IsNullOrEmpty(unevaluatedPropertyValue))
                    {
                        return null;
                    }
                    
                    storedProperties.TargetFrameworkMoniker = unevaluatedPropertyValue;
                    
                    // Only projects targeting .NET 5 or higher use platform properties.
                    string targetFrameworkAlias = await GetTargetFrameworkAliasAsync(unevaluatedPropertyValue);
                    if (!IsNetCore5OrHigher(targetFrameworkAlias))
                    {
                        // Delete platform properties
                        storedProperties.TargetPlatformIdentifier = null;
                        storedProperties.TargetPlatformVersion = null;
                        await ResetPlatformPropertiesAsync(defaultProperties);
                    }
                }

                // Changing the Target Platform Identifier
                else if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetPlatformProperty))
                {
                    if (unevaluatedPropertyValue != storedProperties.TargetPlatformIdentifier)
                    {
                        // Delete platform properties.
                        storedProperties.TargetPlatformVersion = null;
                        await ResetPlatformPropertiesAsync(defaultProperties);

                        storedProperties.TargetPlatformIdentifier = unevaluatedPropertyValue;
                    }
                }

                // Changing the Target Platform Version
                else if (StringComparers.PropertyLiteralValues.Equals(propertyName, TargetPlatformVersionProperty))
                {
                    storedProperties.TargetPlatformVersion = unevaluatedPropertyValue;
                }

                await defaultProperties.SetPropertyValueAsync(TargetFrameworkProperty, await ComputeValueAsync(storedProperties));
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

            return storedValue;
        }

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? storedValue = await GetStoredValueAsync(configuration, propertyName);

            if (storedValue is null)
            {
                return string.Empty;
            }

            return storedValue;
        }

        private static async Task<ComplexTargetFramework> GetStoredComplexTargetFrameworkAsync(ConfigurationGeneral configuration)
        {
            var storedValues = new ComplexTargetFramework
            {
                TargetFrameworkMoniker = (string?)await configuration.TargetFrameworkMoniker.GetValueAsync(),
                TargetPlatformIdentifier = (string?)await configuration.TargetPlatformIdentifier.GetValueAsync(),
                TargetPlatformVersion = (string?)await configuration.TargetPlatformVersion.GetValueAsync(),
                TargetFrameworkIdentifier = (string?)await configuration.TargetFrameworkIdentifier.GetValueAsync(),
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
        /// In order to save the correct value, we make calls to retrieve the alias for the target framework and platform 
        /// (.NET 5.0 => net5.0, .NET Core 3.1 => netcoreapp3.1, Windows => windows).
        /// Examples: 
        /// { .NET 5.0, Windows, 10.0 } => net5.0-windows10.0
        /// { .NET 5.0, null, null } => net5.0
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

            // Check if the project requires an explicit platform.
            if (IsNetCore5OrHigher(targetFrameworkAlias) && await IsWindowsPlatformNeededAsync())
            {
                // Ideally we would set the complexTargetFramework.TargetPlatformIdentifier = "Windows",
                // but we're in a difficult position right now to retrieve the correct TargetPlatformAlias from GetTargetPlatformAliasAsync below,
                // reason being it calls for the previous TargetFramework, not the new one that is being set.
                // Therefore, in the case where we are going from a TargetFramework with no need of platform, like netcoreapp3.1,
                // to one that does, like net5.0, we would be querying the list of TargetPlatformAlias for netcoreapp3.1 and get an empty list.
                // I have to revisit this approach, but for now we can pass the TargetPlatformAlias that should be.
                return targetFrameworkAlias + "-windows" + complexTargetFramework.TargetPlatformVersion;
            }

            // We only keep the platform properties for projects targeting .NET 5 or higher.
            if (!Strings.IsNullOrEmpty(complexTargetFramework.TargetPlatformIdentifier) && IsNetCore5OrHigher(targetFrameworkAlias))
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

        private static bool IsNetCore5OrHigher(string targetFrameworkAlias)
        {
            // Ideally, we want to use the TargetFrameworkIdentifier and TargetFrameworkVersion;
            // however, in this case the target framework properties we have are for the currently set value,
            // not the value we want to set it to (for example, if we go from netcoreapp3.1 to net5.0).
            // The only property we have that describes the value to be set is the TargetFrameworkAlias which
            // is passed to this method.

            Match match = s_versionRegex.Match(targetFrameworkAlias);

            if (match.Success)
            {
                string versionString = match.Groups["version"].Value;

                if (Version.TryParse(versionString, out Version? version))
                {
                    if (version.Major >= 5)
                    {
                        // This is a .NET Core app with version greater than five.
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<bool> IsWindowsPlatformNeededAsync()
        {
            // Checks if the project has either UseWPF or UseWindowsForms properties.
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            _useWPFProperty = (bool?)await configuration.UseWPF.GetValueAsync();

            if (_useWPFProperty is not null)
            {
                return (bool)_useWPFProperty;
            }

            _useWindowsFormsProperty = (bool?)await configuration.UseWindowsForms.GetValueAsync();
            if (_useWindowsFormsProperty is not null)
            {
                return (bool)_useWindowsFormsProperty;
            }

            _useWinUIProperty = (bool?)await configuration.UseWinUI.GetValueAsync();
            if (_useWinUIProperty is not null)
            {
                return (bool)_useWinUIProperty;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the target framework alias (i.e. net5.0) from the project's subscription service.
        /// </summary>
        /// <returns></returns>
        internal virtual async Task<string> GetTargetFrameworkAliasAsync(string targetFrameworkMoniker)
        {
            IProjectSubscriptionService? subscriptionService = _configuredProject.Services.ProjectSubscription;
            Assumes.Present(subscriptionService);

            IImmutableDictionary<string, IProjectRuleSnapshot> supportedTargetFrameworks = await subscriptionService.ProjectRuleSource.GetLatestVersionAsync(_configuredProject, new string[] { SupportedTargetFramework.SchemaName });
            IProjectRuleSnapshot targetFrameworkRuleSnapshot = supportedTargetFrameworks[SupportedTargetFramework.SchemaName];

            IImmutableDictionary<string, string>? targetFrameworkProperties = targetFrameworkRuleSnapshot.GetProjectItemProperties(targetFrameworkMoniker);

            if (targetFrameworkProperties is not null &&
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
        /// Resets the values on the TargetPlatformProperty and SupportedOSPlatformVersionProperty.
        /// </summary>
        /// <param name="projectProperties"></param>
        /// <returns></returns>
        private static async Task ResetPlatformPropertiesAsync(IProjectProperties projectProperties)
        {
            await projectProperties.DeletePropertyAsync(TargetPlatformProperty);
            await projectProperties.DeletePropertyAsync(TargetPlatformVersionProperty);
            await projectProperties.DeletePropertyAsync(SupportedOSPlatformVersionProperty);
        }
    }
}
