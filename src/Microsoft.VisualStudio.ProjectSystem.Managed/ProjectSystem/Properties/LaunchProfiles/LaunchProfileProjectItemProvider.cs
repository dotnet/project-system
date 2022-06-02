// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Exposes launch profiles (from launchSettings.json files) as CPS <see cref="IProjectItem"/>s,
    /// where each launch profile becomes its own <see cref="IProjectItem"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type mostly delegates to the <see cref="ILaunchSettingsProvider"/>, and serves to translate
    /// its abilities to the <see cref="IProjectItemProvider"/> model.
    /// </para>
    /// <para>
    /// <see cref="IProjectItem"/> support, like many things in CPS, is designed with MSBuild concepts
    /// in mind (e.g., evaluated vs. unevaluated data, items/properties possibly being defined in
    /// imported files rather than the project itself, etc.) and many of these do not translate
    /// directly to launch profiles. Additional details can be found in the remarks on the individual
    /// type members.
    /// </para>
    /// </remarks>
    [Export(typeof(IProjectItemProvider))]
    [ExportMetadata("Name", "LaunchProfile")]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchProfileProjectItemProvider : IProjectItemProvider
    {
        /// <remarks>
        /// <para>
        /// Launch profiles will be exposed with this item type.
        /// </para>
        /// <para>
        /// As an alternative, we could use the debug command associated with
        /// a launch profile as the basis for the item type. That is, rather than
        /// all launch profiles having the same item type, all launch profiles
        /// with the same debug command have the same item type. In that design,
        /// the set of item types returned by <see cref="GetItemTypesAsync"/>
        /// would be based on the set of debug command handlers. This approach
        /// might make it easier to bind an item presenting a launch profile to
        /// related Rules, but at this time it it not clear that it is necessary.
        /// </para>
        /// </remarks>
        public static string ItemType = "LaunchProfile";

        private static readonly ImmutableSortedSet<string> s_itemTypes = ImmutableSortedSet.Create(StringComparers.ItemTypes, ItemType);
        private readonly UnconfiguredProject _project;
        private readonly ILaunchSettingsProvider _launchSettingsProvider;

#pragma warning disable CS0067 // Unused event

        /// <remarks>
        /// This would fire when an item is about to be renamed, but this provider does not
        /// support renaming a launch profile. As such, this event will never be invoked.
        /// </remarks>
        public event AsyncEventHandler<ItemIdentityChangedEventArgs>? ItemIdentityChanging;

        /// <remarks>
        /// This would fire after an item is has been renamed, but this provider does not
        /// support renaming a launch profile. As such, this event will never be invoked.
        /// </remarks>
        public event AsyncEventHandler<ItemIdentityChangedEventArgs>? ItemIdentityChangedOnWriter;

        /// <remarks>
        /// This would fire after an item is has been renamed, but this provider does not
        /// support renaming a launch profile. As such, this event will never be invoked.
        /// </remarks>
        public event AsyncEventHandler<ItemIdentityChangedEventArgs>? ItemIdentityChanged;

#pragma warning restore CS0067

        [ImportingConstructor]
        public LaunchProfileProjectItemProvider(UnconfiguredProject project, ILaunchSettingsProvider launchSettingsProvider)
        {
            _project = project;
            _launchSettingsProvider = launchSettingsProvider;
        }

        public async Task<IProjectItem?> AddAsync(string itemType, string include, IEnumerable<KeyValuePair<string, string>>? metadata = null)
        {
            if (!StringComparers.ItemTypes.Equals(itemType, ItemType))
            {
                throw new ArgumentException($"The {nameof(LaunchProfileProjectItemProvider)} does not handle the '{itemType}' item type.");
            }

            WritableLaunchProfile newLaunchProfile = new() { Name = include };
            await _launchSettingsProvider.AddOrUpdateProfileAsync(newLaunchProfile.ToLaunchProfile(), addToFront: false);

            // TODO: set the metadata on the launch profile.

            return await FindItemByNameAsync(include);
        }

        /// <remarks>
        /// As this adds multiple items we should consider adding them all as an atomic operation.
        /// However, <see cref="ILaunchSettingsProvider"/> currently provides no way of doing that.
        /// </remarks>
        public async Task<IEnumerable<IProjectItem>> AddAsync(IEnumerable<Tuple<string, string, IEnumerable<KeyValuePair<string, string>>?>> items)
        {
            List<IProjectItem> projectItems = new();

            foreach ((string itemType, string include, IEnumerable<KeyValuePair<string, string>>? metadata) in items)
            {
                IProjectItem? projectItem = await AddAsync(itemType, include, metadata);
                if (projectItem is not null)
                {
                    projectItems.Add(projectItem);
                }
            }

            return projectItems;
        }

        /// <remarks>
        /// Launch profiles do not represent a file on disk, so adding one via a path is
        /// a meaningless operation.
        /// </remarks>
        public Task<IProjectItem> AddAsync(string path)
        {
            throw new InvalidOperationException($"The {nameof(LaunchProfileProjectItemProvider)} does not support adding items as paths.");
        }

        /// <remarks>
        /// Launch profiles do not represent a file on disk, so adding one via a path is
        /// a meaningless operation.
        /// </remarks>
        public Task<IReadOnlyList<IProjectItem>> AddAsync(IEnumerable<string> paths)
        {
            throw new InvalidOperationException($"The {nameof(LaunchProfileProjectItemProvider)} does not support adding items as paths.");
        }

        public async Task<IProjectItem?> FindItemByNameAsync(string evaluatedInclude)
        {
            IEnumerable<IProjectItem> items = await GetItemsAsync();
            return items.FirstOrDefault(item => StringComparers.ItemNames.Equals(item.EvaluatedInclude, evaluatedInclude));
        }

        public async Task<IImmutableSet<string>> GetExistingItemTypesAsync()
        {
            ILaunchSettings snapshot = await _launchSettingsProvider.WaitForFirstSnapshot();

            return snapshot.Profiles.Count > 0
                ? s_itemTypes
                : ImmutableSortedSet<string>.Empty;
        }

        public async Task<IProjectItem?> GetItemAsync(IProjectPropertiesContext context)
        {
            if (context.ItemType is not null
                && !StringComparers.ItemTypes.Equals(ItemType, context.ItemType))
            {
                return null;
            }

            IEnumerable<IProjectItem> items = await GetItemsAsync();
            return items.FirstOrDefault(item => StringComparers.ItemNames.Equals(item.EvaluatedInclude, context.ItemName));
        }

        public async Task<IEnumerable<IProjectItem>> GetItemsAsync()
        {
            ILaunchSettings snapshot = await _launchSettingsProvider.WaitForFirstSnapshot();

            if (snapshot.Profiles.Count == 0)
            {
                return Enumerable.Empty<IProjectItem>();
            }

            return snapshot.Profiles.Select(p => new ProjectItem(p.Name ?? string.Empty, _project.FullPath, this));
        }

        public Task<IEnumerable<IProjectItem>> GetItemsAsync(string itemType)
        {
            if (StringComparers.ItemTypes.Equals(itemType, ItemType))
            {
                return GetItemsAsync();
            }

            return TaskResult.EmptyEnumerable<IProjectItem>();
        }

        public async Task<IEnumerable<IProjectItem>> GetItemsAsync(string itemType, string evaluatedInclude)
        {
            IEnumerable<IProjectItem> items = await GetItemsAsync(itemType);

            return items.Where(item => StringComparers.ItemNames.Equals(item.EvaluatedInclude, evaluatedInclude));
        }

        public async Task<IReadOnlyCollection<IProjectItem?>> GetItemsAsync(IReadOnlyCollection<IProjectPropertiesContext> contexts)
        {
            ImmutableArray<IProjectItem?>.Builder builder = ImmutableArray.CreateBuilder<IProjectItem?>(initialCapacity: contexts.Count);

            foreach (IProjectPropertiesContext context in contexts)
            {
                builder.Add(await GetItemAsync(context));
            }

            return builder.MoveToImmutable();
        }

        public Task<IImmutableSet<string>> GetItemTypesAsync()
        {
            return Task.FromResult<IImmutableSet<string>>(s_itemTypes);
        }

        public async Task RemoveAsync(string itemType, string include, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            if (!StringComparers.ItemTypes.Equals(itemType, ItemType))
            {
                return;
            }

            await _launchSettingsProvider.RemoveProfileAsync(include);
        }

        public Task RemoveAsync(IProjectItem item, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            Assumes.NotNull(item.ItemType);
            return RemoveAsync(item.ItemType, item.UnevaluatedInclude, deleteOptions);
        }

        /// <remarks>
        /// As this removes multiple items we should consider adding them all as an atomic operation.
        /// However, <see cref="ILaunchSettingsProvider"/> currently provides no way of doing that.
        /// </remarks>
        public async Task RemoveAsync(IEnumerable<IProjectItem> items, DeleteOptions deleteOptions = DeleteOptions.None)
        {
            foreach (IProjectItem item in items)
            {
                await RemoveAsync(item, deleteOptions);
            }
        }

        /// <remarks>
        /// Effectively, setting the Include on an item renames that item. <see cref="ILaunchSettingsProvider"/> does
        /// not natively support renaming a profile; we should consider adding that functionality or "faking" it via
        /// a remove and add. In the latter case we would want this to be an atomic operation.
        /// </remarks>
        public Task SetUnevaluatedIncludesAsync(IReadOnlyCollection<KeyValuePair<IProjectItem, string>> renames)
        {
            throw new InvalidOperationException($"The {nameof(LaunchProfileProjectItemProvider)} does not support renaming items.");
        }

        /// <summary>
        /// An implementation of <see cref="IProjectItem"/> that represents an individual launch profile.
        /// </summary>
        private class ProjectItem : IProjectItem
        {
            private readonly string _name;
            private readonly string _projectFilePath;
            private readonly LaunchProfileProjectItemProvider _provider;

            public ProjectItem(string name, string projectFilePath, LaunchProfileProjectItemProvider provider)
            {
                _name = name;
                _projectFilePath = projectFilePath;
                _provider = provider;

                PropertiesContext = new ProjectPropertiesContext(name, projectFilePath);
            }

            public string ItemType => LaunchProfileProjectItemProvider.ItemType;

            /// <remarks>
            /// Launch profiles have no concept of evaluation, so the evaluated and unevaluated
            /// names are the same.
            /// </remarks>
            public string UnevaluatedInclude => _name;

            /// <remarks>
            /// Launch profiles have no concept of evaluation, so the evaluated and unevaluated
            /// names are the same.
            /// </remarks>
            public string EvaluatedInclude => _name;

            /// <remarks>
            /// Launch profiles represent an entry in the launchSettings.json file rather than a
            /// file on disk; as such there is no meaningful value we can return here.
            /// </remarks>
            public string EvaluatedIncludeAsFullPath => string.Empty;

            /// <remarks>
            /// Launch profiles represent an entry in the launchSettings.json file rather than a
            /// file on disk; as such there is no meaningful value we can return here.
            /// </remarks>
            public string EvaluatedIncludeAsRelativePath => string.Empty;

            public IProjectPropertiesContext PropertiesContext { get; }

            public IProjectProperties Metadata => throw new NotImplementedException();

            public Task RemoveAsync(DeleteOptions deleteOptions = DeleteOptions.None)
            {
                return _provider.RemoveAsync(this, deleteOptions);
            }

            /// <remarks>
            /// The <see cref="LaunchProfileProjectItemProvider"/> only supports one item type and
            /// there is no meaningful "conversion" to other item types, so we don't allow this
            /// operation.
            /// </remarks>
            public Task SetItemTypeAsync(string value)
            {
                throw new InvalidOperationException($"The {nameof(LaunchProfileProjectItemProvider)} does not support changing item types.");
            }

            /// <remarks>
            /// We don't support renaming a launch profile.
            /// </remarks>
            public Task SetUnevaluatedIncludeAsync(string value)
            {
                throw new InvalidOperationException($"The {nameof(LaunchProfileProjectItemProvider)} does not support renaming items.");
            }

            /// <summary>
            /// Implementation of <see cref="IProjectPropertiesContext"/> that represents a specific launch
            /// profile.
            /// </summary>
            private class ProjectPropertiesContext : IProjectPropertiesContext
            {
                private readonly string _name;
                private readonly string _projectFilePath;

                public ProjectPropertiesContext(string name, string projectFilePath)
                {
                    _name = name;
                    _projectFilePath = projectFilePath;
                }

                /// <remarks>
                /// Launch profiles can only appear in the launchSettings.json, and there can only be
                /// one launchSettings.json per project. As such, all profiles are logically part of
                /// the project file.
                /// </remarks>
                public bool IsProjectFile => true;

                /// <remarks>
                /// Logically, profiles belong to the project file and can only belong to the project
                /// file, as opposed to an imported file.
                /// </remarks>
                public string File => _projectFilePath;

                public string? ItemType => LaunchProfileProjectItemProvider.ItemType;

                public string? ItemName => _name;
            }
        }
    }
}
