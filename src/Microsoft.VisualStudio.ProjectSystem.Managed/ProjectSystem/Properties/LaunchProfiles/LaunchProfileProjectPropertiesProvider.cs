// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("LaunchProfile", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "LaunchProfile")]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal partial class LaunchProfileProjectPropertiesProvider : IProjectPropertiesProvider
    {
        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider3 _launchSettingsProvider;
        private readonly ImmutableArray<Lazy<ILaunchProfileExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> _launchProfileExtensionValueProviders;
        private readonly ImmutableArray<Lazy<IGlobalSettingExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> _globalSettingExtensionValueProviders;

        [ImportingConstructor]
        public LaunchProfileProjectPropertiesProvider(
            UnconfiguredProject project,
            ILaunchSettingsProvider3 launchSettingsProvider,
            [ImportMany]IEnumerable<Lazy<ILaunchProfileExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> launchProfileExtensionValueProviders,
            [ImportMany]IEnumerable<Lazy<IGlobalSettingExtensionValueProvider, ILaunchProfileExtensionValueProviderMetadata>> globalSettingExtensionValueProviders)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
            _launchProfileExtensionValueProviders = launchProfileExtensionValueProviders.ToImmutableArray();
            _globalSettingExtensionValueProviders = globalSettingExtensionValueProviders.ToImmutableArray();
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
                    && itemType != LaunchProfileProjectItemProvider.ItemType)
                || !StringComparers.Paths.Equals(_project.FullPath, file))
            {
                // The interface is CPS currently asserts that the Get*Properties methods return a
                // non-null value, but this is incorrect--in practice the implementations do return
                // null.
                return null!;
            }

            return new LaunchProfileProjectProperties(file, item, _launchSettingsProvider, _launchProfileExtensionValueProviders, _globalSettingExtensionValueProviders);
        }
    }
}
