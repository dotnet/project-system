// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using LaunchProfileValueProviderAndMetadata = System.Lazy<
    Microsoft.VisualStudio.ProjectSystem.Debug.ILaunchProfileExtensionValueProvider,
    Microsoft.VisualStudio.ProjectSystem.Debug.ILaunchProfileExtensionValueProviderMetadata>;
using GlobalSettingValueProviderAndMetadata = System.Lazy<
    Microsoft.VisualStudio.ProjectSystem.Debug.IGlobalSettingExtensionValueProvider,
    Microsoft.VisualStudio.ProjectSystem.Debug.ILaunchProfileExtensionValueProviderMetadata>;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class LaunchProfileProjectProperties : IProjectProperties
    {
        private const string CommandNamePropertyName = "CommandName";
        private const string ExecutablePathPropertyName = "ExecutablePath";
        private const string CommandLineArgumentsPropertyName = "CommandLineArguments";
        private const string WorkingDirectoryPropertyName = "WorkingDirectory";
        private const string LaunchBrowserPropertyName = "LaunchBrowser";
        private const string LaunchUrlPropertyName = "LaunchUrl";
        private const string EnvironmentVariablesPropertyName = "EnvironmentVariables";

        /// <remarks>
        /// These correspond to the properties explicitly declared on <see cref="ILaunchProfile"/>
        /// and as such they are always considered to exist on the profile, though they may
        /// not have a value.
        /// </remarks>
        private static readonly string[] s_standardPropertyNames = new[]
        {
            CommandNamePropertyName,
            ExecutablePathPropertyName,
            CommandLineArgumentsPropertyName,
            WorkingDirectoryPropertyName,
            LaunchBrowserPropertyName,
            LaunchUrlPropertyName,
            EnvironmentVariablesPropertyName
        };

        private readonly LaunchProfilePropertiesContext _context;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;
        private readonly ImmutableDictionary<string, LaunchProfileValueProviderAndMetadata> _launchProfileValueProviders;
        private readonly ImmutableDictionary<string, GlobalSettingValueProviderAndMetadata> _globalSettingValueProviders;

        public LaunchProfileProjectProperties(
            string filePath,
            string profileName,
            ILaunchSettingsProvider launchSettingsProvider,
            ImmutableArray<LaunchProfileValueProviderAndMetadata> launchProfileExtensionValueProviders,
            ImmutableArray<GlobalSettingValueProviderAndMetadata> globalSettingExtensionValueProviders)
        {
            _context = new LaunchProfilePropertiesContext(filePath, profileName);
            _launchSettingsProvider = launchSettingsProvider;

            ImmutableDictionary<string, LaunchProfileValueProviderAndMetadata>.Builder launchProfileValueBuilder =
                ImmutableDictionary.CreateBuilder<string, LaunchProfileValueProviderAndMetadata>(StringComparers.PropertyNames);
            foreach (LaunchProfileValueProviderAndMetadata valueProvider in launchProfileExtensionValueProviders)
            {
                string[] propertyNames = valueProvider.Metadata.PropertyNames;

                foreach (string propertyName in propertyNames)
                {
                    Requires.Argument(!string.IsNullOrEmpty(propertyName), nameof(valueProvider), "A null or empty property name was found");

                    // CONSIDER: Allow duplicate intercepting property value providers for same property name.
                    Requires.Argument(!launchProfileValueBuilder.ContainsKey(propertyName), nameof(launchProfileValueBuilder), "Duplicate property value providers for same property name");

                    launchProfileValueBuilder.Add(propertyName, valueProvider);
                }
            }
            _launchProfileValueProviders = launchProfileValueBuilder.ToImmutable();

            ImmutableDictionary<string, GlobalSettingValueProviderAndMetadata>.Builder globalSettingValueBuilder =
                ImmutableDictionary.CreateBuilder<string, GlobalSettingValueProviderAndMetadata>(StringComparers.PropertyNames);
            foreach (GlobalSettingValueProviderAndMetadata valueProvider in globalSettingExtensionValueProviders)
            {
                string[] propertyNames = valueProvider.Metadata.PropertyNames;

                foreach (string propertyName in propertyNames)
                {
                    Requires.Argument(!string.IsNullOrEmpty(propertyName), nameof(valueProvider), "A null or empty property name was found");

                    // CONSIDER: Allow duplicate intercepting property value providers for same property name.
                    Requires.Argument(!globalSettingValueBuilder.ContainsKey(propertyName), nameof(globalSettingValueBuilder), "Duplicate property value providers for same property name");

                    globalSettingValueBuilder.Add(propertyName, valueProvider);
                }
            }
            _globalSettingValueProviders = globalSettingValueBuilder.ToImmutable();
        }

        public IProjectPropertiesContext Context => _context;

        public string FileFullPath => _context.File;

        public PropertyKind PropertyKind => PropertyKind.ItemGroup;

        public Task DeleteDirectPropertiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetDirectPropertyNamesAsync()
        {
            return GetPropertyNamesAsync();
        }

        public async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            return await GetUnevaluatedPropertyValueAsync(propertyName) ?? string.Empty;
        }

        /// <remarks>
        /// If the profile exists we return all the standard property names (as they are
        /// always considered defined) plus all of the defined properties supported by
        /// extenders.
        /// </remarks>
        public async Task<IEnumerable<string>> GetPropertyNamesAsync()
        {
            ILaunchSettings? snapshot = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
            Assumes.NotNull(snapshot);

            ILaunchProfile? profile = snapshot.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, _context.ItemName));
            if (profile is null)
            {
                return Enumerable.Empty<string>();
            }
            ImmutableDictionary<string, object> globalSettings = snapshot.GlobalSettings;

            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.CreateBuilder<string>(StringComparers.PropertyNames);
            builder.UnionWith(s_standardPropertyNames);

            foreach ((string propertyName, LaunchProfileValueProviderAndMetadata provider) in _launchProfileValueProviders)
            {
                // TODO: Pass the Rule
                string propertyValue = await provider.Value.OnGetPropertyValueAsync(propertyName, profile, globalSettings, rule: null);
                if (!Strings.IsNullOrEmpty(propertyValue))
                {
                    builder.Add(propertyName);
                }
            }

            foreach ((string propertyName, GlobalSettingValueProviderAndMetadata provider) in _globalSettingValueProviders)
            {
                // TODO: Pass the Rule
                string propertyValue = await provider.Value.OnGetPropertyValueAsync(propertyName, globalSettings, rule: null);
                if (!Strings.IsNullOrEmpty(propertyValue))
                {
                    builder.Add(propertyName);
                }
            }

            return builder.ToImmutable();
        }

        /// <returns>
        /// If the profile does not exist, returns <see langword="null"/>. Otherwise, returns the value
        /// of the property if the property is not defined, or <see langword="null"/> otherwise. The
        /// standard properties are always considered to be defined.
        /// </returns>
        public async Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            ILaunchSettings? snapshot = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
            Assumes.NotNull(snapshot);

            ILaunchProfile? profile = snapshot.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, _context.ItemName));
            if (profile is null)
            {
                return null;
            }

            return propertyName switch
            {
                CommandNamePropertyName => profile.CommandName ?? string.Empty,
                ExecutablePathPropertyName => profile.ExecutablePath ?? string.Empty,
                CommandLineArgumentsPropertyName => profile.CommandLineArgs ?? string.Empty,
                WorkingDirectoryPropertyName => profile.WorkingDirectory ?? string.Empty,
                LaunchBrowserPropertyName => profile.LaunchBrowser ? "true" : "false",
                LaunchUrlPropertyName => profile.LaunchUrl ?? string.Empty,
                EnvironmentVariablesPropertyName => ConvertDictionaryToString(profile.EnvironmentVariables) ?? string.Empty,
                _ => await GetPropertyValueFromExtendersAsync(propertyName, profile, snapshot.GlobalSettings)
            };
        }

        public Task<bool> IsValueInheritedAsync(string propertyName)
        {
            return TaskResult.False;
        }

        public Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            throw new NotImplementedException();
        }

        private async Task<string?> GetPropertyValueFromExtendersAsync(string propertyName, ILaunchProfile profile, ImmutableDictionary<string, object> globalSettings)
        {
            if (_launchProfileValueProviders.TryGetValue(propertyName, out LaunchProfileValueProviderAndMetadata? launchProfileValueProvider))
            {
                // TODO: Pass the Rule
                return await launchProfileValueProvider.Value.OnGetPropertyValueAsync(propertyName, profile, globalSettings, rule: null);
            }

            if (_globalSettingValueProviders.TryGetValue(propertyName, out GlobalSettingValueProviderAndMetadata? globalSettingValueProvider))
            {
                // TODO: Pass the Rule
                return await globalSettingValueProvider.Value.OnGetPropertyValueAsync(propertyName, globalSettings, rule: null);
            }

            return null;
        }

        private static string? ConvertDictionaryToString(ImmutableDictionary<string, string>? value)
        {
            if (value is null)
            {
                return null;
            }

            return string.Join(",", value.OrderBy(kvp => kvp.Key, StringComparer.Ordinal).Select(kvp => $"{encode(kvp.Key)}={encode(kvp.Value)}"));

            static string encode(string value)
            {
                return value.Replace("/", "//").Replace(",", "/,").Replace("=", "/=");
            }
        }

        private class LaunchProfilePropertiesContext : IProjectPropertiesContext
        {
            public LaunchProfilePropertiesContext(string file, string itemName)
            {
                File = file;
                ItemName = itemName;
            }

            public bool IsProjectFile => true;

            public string File { get; }

            public string ItemType => LaunchProfileProjectItemProvider.ItemType;

            public string ItemName { get; }
        }
    }
}
