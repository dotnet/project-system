// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [Export("LaunchProfiles", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "LaunchProfiles")]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchProfilesProjectPropertiesProvider : IProjectPropertiesProvider
    {
        /// <remarks>
        /// These correspond to the properties explicitly declared on <see cref="ILaunchProfile"/>
        /// and as such they are always considered to exist on the profile, though they may
        /// not have a value.
        /// </remarks>
        private static readonly string[] s_standardPropertyNames = new[]
        {
            "CommandName",
            "ExecutablePath",
            "CommandLineArgs",
            "WorkingDirectory",
            "LaunchBrowser",
            "LaunchUrl",
            "EnvironmentVariables"
        };

        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;

        [ImportingConstructor]
        public LaunchProfilesProjectPropertiesProvider(UnconfiguredProject project,
            ILaunchSettingsProvider launchSettingsProvider)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
        }

        /// <remarks>
        /// There is a 1:1 mapping between launchSettings.json and the related project, and
        /// launch profiles can't come from anywhere else (i.e., there's no equivalent of
        /// imported .props and .targets files for launch settings). As such, launch profiles
        /// are stored in the launchSettings.json file, but the logical context is the
        /// project file.
        /// </remarks>
        public string DefaultProjectPath => _project.FullPath;

#pragma warning disable CS0067 // Unused events
        // Currently nothing consumes these, so we don't need to worry about firing them.
        // This is great because they are supposed to offer guarantees to the event
        // handlers about what locks are held--but the launch profiles don't use such
        // locks.
        // If in the future we determine that we need to fire these we will either need to
        // work through the implications on the project locks, or we will need to decide
        // if we can only fire a subset of these events.
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanging;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChangedOnWriter;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs>? ProjectPropertyChanged;
#pragma warning restore CS0067

        /// <remarks>
        /// <para>
        /// There are no "project-level" properties supported by the launch profiles. We
        /// return an implementation of <see cref="IProjectProperties"/> for the sake of
        /// completeness but there isn't much the consumer can do with it. We could also
        /// consider just throwing a <see cref="NotSupportedException"/> for this method,
        /// but the impact of that isn't clear.
        /// </para>
        /// <para>
        /// Note that the launch settings do support the concept of global properties, but
        /// we're choosing to expose those as if they were properties on the individual
        /// launch profiles.
        /// </para>
        /// </remarks>
        public IProjectProperties GetCommonProperties()
        {
            return GetProperties(_project.FullPath, itemType: null, item: null);
        }

        public IProjectProperties GetItemProperties(string? itemType, string? item)
        {
            return GetProperties(_project.FullPath, itemType, item);
        }

        public IProjectProperties GetItemTypeProperties(string? itemType)
        {
            return GetProperties(_project.FullPath, itemType, item: null);
        }

        public IProjectProperties GetProperties(string file, string? itemType, string? item)
        {
            if (item is null
                || (itemType is not null
                    && itemType != LaunchProfilesProjectItemProvider.ItemType)
                || !StringComparers.Paths.Equals(_project.FullPath, file))
            {
                // The interface is CPS currently asserts that the Get*Properties methods return a
                // non-null value, but this is incorrect--in practice the implementations do return
                // null.
                return null!;
            }

            return new LaunchProfileProperties(file, item, this);
        }

        /// <remarks>
        /// If the profile exists we return all the standard property names (as they are
        /// always considered defined) plus all of the defined properties supported by
        /// extenders.
        /// </remarks>
        private async Task<IEnumerable<string>> GetPropertyNamesAsync(string profileName)
        {
            ILaunchSettings? snapshot = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
            Assumes.NotNull(snapshot);

            ILaunchProfile? profile = snapshot.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, profileName));
            if (profile is null)
            {
                return Enumerable.Empty<string>();
            }

            return s_standardPropertyNames;

            // TODO: Handle property names supported by launch profile extenders.
        }

        private async Task<string> GetEvaluatedPropertyValueAsync(string itemName, string propertyName)
        {
            return await GetUnevaluatedPropertyValueAsync(itemName, propertyName) ?? string.Empty;
        }

        /// <returns>
        /// If the profile does not exist, returns <c>null</c>. Otherwise, returns the value
        /// of the property if the property is not defined, or <c>null</c> otherwise. The
        /// standard properties are always considered to be defined.
        /// </returns>
        private async Task<string?> GetUnevaluatedPropertyValueAsync(string profileName, string propertyName)
        {
            ILaunchSettings? snapshot = await _launchSettingsProvider.WaitForFirstSnapshot(Timeout.Infinite);
            Assumes.NotNull(snapshot);

            ILaunchProfile? profile = snapshot.Profiles.FirstOrDefault(p => StringComparers.LaunchProfileNames.Equals(p.Name, profileName));
            if (profile is null)
            {
                return null;
            }

            return propertyName switch
            {
                "CommandName" => profile.CommandName ?? string.Empty,
                "ExecutablePath" => profile.ExecutablePath ?? string.Empty,
                "CommandLineArgs" => profile.CommandLineArgs ?? string.Empty,
                "WorkingDirectory" => profile.WorkingDirectory ?? string.Empty,
                "LaunchBrowser" => profile.LaunchBrowser ? "true" : "false",
                "LaunchUrl" => profile.LaunchUrl ?? string.Empty,
                "EnvironmentVariables" => ConvertDictionaryToString(profile.EnvironmentVariables) ?? string.Empty,
                _ => null
                // TODO: Handle properties supported by launch profile extenders.
            };
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

        private class LaunchProfileProperties : IProjectProperties
        {
            private readonly LaunchProfilePropertiesContext _context;
            private readonly LaunchProfilesProjectPropertiesProvider _provider;

            public LaunchProfileProperties(string filePath, string profileName, LaunchProfilesProjectPropertiesProvider provider)
            {
                _context = new LaunchProfilePropertiesContext(filePath, profileName);
                _provider = provider;
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

            public Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            {
                return _provider.GetEvaluatedPropertyValueAsync(_context.ItemName, propertyName);
            }

            public Task<IEnumerable<string>> GetPropertyNamesAsync()
            {
                return _provider.GetPropertyNamesAsync(_context.ItemName);
            }

            public Task<string?> GetUnevaluatedPropertyValueAsync(string propertyName)
            {
                return _provider.GetUnevaluatedPropertyValueAsync(_context.ItemName, propertyName);
            }

            public Task<bool> IsValueInheritedAsync(string propertyName)
            {
                return TaskResult.False;
            }

            public Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
            {
                throw new NotImplementedException();
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

                public string ItemType => LaunchProfilesProjectItemProvider.ItemType;

                public string ItemName { get; }
            }
        }
    }
}
