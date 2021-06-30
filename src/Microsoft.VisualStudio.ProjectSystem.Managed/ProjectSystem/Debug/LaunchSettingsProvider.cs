// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.SpecialFileProviders;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// Manages the set of Debug profiles and web server settings and provides these as a dataflow source. Note
    /// that many of the methods are protected so that unit tests can derive from this class and poke them as
    /// needed w/o making them public
    /// </summary>
    [Export(typeof(ILaunchSettingsProvider))]
    [Export(typeof(ILaunchSettingsProvider2))]
    [Export(typeof(ILaunchSettingsProvider3))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchSettingsProvider : OnceInitializedOnceDisposed, ILaunchSettingsProvider3
    {
        public const string LaunchSettingsFilename = "launchSettings.json";
        public const string ProfilesSectionName = "profiles";

        // Command that means run this project
        public const string RunProjectCommandName = "Project";

        //  Command that means run an executable
        public const string RunExecutableCommandName = "Executable";

        // These are used internally to loop in debuggers to handle F5 when there are errors in
        // the launch settings file or when there are no profiles specified (like class libraries)
        public const string ErrorProfileCommandName = "ErrorProfile";

        private readonly UnconfiguredProject _project;
        private readonly IActiveConfiguredValue<IAppDesignerFolderSpecialFileProvider?> _appDesignerSpecialFileProvider;
        private readonly IProjectFaultHandlerService _projectFaultHandler;
        private readonly AsyncLazy<string> _launchSettingsFilePath;
        private readonly IUnconfiguredProjectServices _projectServices;
        private readonly IUnconfiguredProjectCommonServices _commonProjectServices;
        private readonly IActiveConfiguredProjectSubscriptionService? _projectSubscriptionService;
        private readonly IFileSystem _fileSystem;
        private readonly TaskCompletionSource _firstSnapshotCompletionSource = new();
        private readonly SequentialTaskExecutor _sequentialTaskQueue = new();
        private readonly Lazy<ILaunchProfile?> _defaultLaunchProfile;
        private IReceivableSourceBlock<ILaunchSettings>? _changedSourceBlock;
        private IBroadcastBlock<ILaunchSettings>? _broadcastBlock;
        private ILaunchSettings? _currentSnapshot;
        private IDisposable? _projectRuleSubscriptionLink;
        private long _nextVersion;

        [ImportingConstructor]
        public LaunchSettingsProvider(
            UnconfiguredProject project,
            IUnconfiguredProjectServices projectServices,
            IFileSystem fileSystem,
            IUnconfiguredProjectCommonServices commonProjectServices,
            IActiveConfiguredProjectSubscriptionService? projectSubscriptionService,
            IActiveConfiguredValue<IAppDesignerFolderSpecialFileProvider?> appDesignerSpecialFileProvider,
            IProjectFaultHandlerService projectFaultHandler)
        {
            _project = project;
            _projectServices = projectServices;
            _fileSystem = fileSystem;
            _commonProjectServices = commonProjectServices;
            JsonSerializationProviders = new OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                                                                                                                    project);
            SourceControlIntegrations = new OrderPrecedenceImportCollection<ISourceCodeControlIntegration>(projectCapabilityCheckProvider: project);

            DefaultLaunchProfileProviders = new OrderPrecedenceImportCollection<IDefaultLaunchProfileProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, project);

            _projectSubscriptionService = projectSubscriptionService;
            _appDesignerSpecialFileProvider = appDesignerSpecialFileProvider;
            _projectFaultHandler = projectFaultHandler;
            _launchSettingsFilePath = new AsyncLazy<string>(GetLaunchSettingsFilePathNoCacheAsync, commonProjectServices.ThreadingService.JoinableTaskFactory);

            _defaultLaunchProfile = new Lazy<ILaunchProfile?> (() => {
                    return DefaultLaunchProfileProviders?.FirstOrDefault()?.Value?.CreateDefaultProfile();
                }
            );

            _nextVersion = 1;
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> JsonSerializationProviders { get; set; }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ISourceCodeControlIntegration> SourceControlIntegrations { get; set; }

        [ImportMany]
        protected OrderPrecedenceImportCollection<IDefaultLaunchProfileProvider> DefaultLaunchProfileProviders { get; set; }

        protected SimpleFileWatcher? FileWatcher { get; set; }

        // When we are saving the file we set this to minimize noise from the file change
        protected bool IgnoreFileChanges { get; set; }

        protected TimeSpan FileChangeProcessingDelay = TimeSpan.FromMilliseconds(500);

        public ITaskDelayScheduler? FileChangeScheduler { get; protected set; }

        // Tracks when we last read or wrote to the file. Prevents picking up needless changes
        protected DateTime LastSettingsFileSyncTimeUtc { get; set; }

        protected const int WaitForFirstSnapshotDelayMillis = 5000;

        [Obsolete("Use GetLaunchSettingsFilePathAsync instead.")]
        public string LaunchSettingsFile => _commonProjectServices.ThreadingService.ExecuteSynchronously(GetLaunchSettingsFilePathAsync);

        /// <summary>
        /// Returns the active profile. Looks up the value of the ActiveProfile property. If the value doesn't match the
        /// any of the profiles, the first one is returned
        /// </summary>
        public ILaunchProfile? ActiveProfile => CurrentSnapshot?.ActiveProfile;

        /// <summary>
        /// Link to this source block to be notified when the snapshot is changed.
        /// </summary>
        public IReceivableSourceBlock<ILaunchSettings> SourceBlock
        {
            get
            {
                EnsureInitialized();
                return _changedSourceBlock!;
            }
        }

        /// <summary>
        /// IDebugProfileProvider
        /// Access to the current set of profile information
        /// </summary>
        public ILaunchSettings CurrentSnapshot
        {
            get
            {
                EnsureInitialized();
                return _currentSnapshot!;
            }
            protected set
            {
                ILaunchSettings? prior = _currentSnapshot;
                _currentSnapshot = value;

                // If this is the first snapshot, complete the taskCompletionSource
                if (prior is null)
                {
                    _firstSnapshotCompletionSource.TrySetResult();
                }
            }
        }

        /// <summary>
        /// The LaunchSettingsProvider sinks 2 sets of information:
        /// 1. Changes to the launchsettings.json file on disk
        /// 2. Changes to the ActiveDebugProfile property in the .user file
        /// </summary>
        protected override void Initialize()
        {
            // Create our broadcast block for subscribers to get new ILaunchProfiles Information
            _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<ILaunchSettings>();
            _changedSourceBlock = _broadcastBlock.SafePublicize();

            // Subscribe to changes to the broadcast block using the idle scheduler. This should filter out a lot of the intermediate
            // states that files can be in.
            if (_projectSubscriptionService != null)
            {
                // The use of AsyncLazy with dataflow can allow state stored in the execution context to leak through. The downstream affect is
                // calls to say, get properties, may fail. To avoid this, we capture the execution context here, and it will be reapplied when
                // we get new subscription data from the dataflow.
                ITargetBlock<IProjectVersionedValue<ValueTuple<IProjectSubscriptionUpdate, IProjectCapabilitiesSnapshot>>> projectChangesBlock = DataflowBlockFactory.CreateActionBlock(
                            DataflowUtilities.CaptureAndApplyExecutionContext<IProjectVersionedValue<ValueTuple<IProjectSubscriptionUpdate, IProjectCapabilitiesSnapshot>>>(ProjectRuleBlock_ChangedAsync), _project, ProjectFaultSeverity.LimitedFunctionality);
                StandardRuleDataflowLinkOptions evaluationLinkOptions = DataflowOption.WithRuleNames(ProjectDebugger.SchemaName);

                _projectRuleSubscriptionLink = ProjectDataSources.SyncLinkTo(
                    _projectSubscriptionService.ProjectRuleSource.SourceBlock.SyncLinkOptions(evaluationLinkOptions),
                    _commonProjectServices.Project.Capabilities.SourceBlock.SyncLinkOptions(),
                    projectChangesBlock,
                    linkOptions: DataflowOption.PropagateCompletion);
            }

            // Make sure we are watching the file at this point
            WatchLaunchSettingsFile();
        }

        /// <summary>
        /// Handles changes to the ProjectDebugger properties. Gets the active profile and generates a launch settings update if it
        /// has changed. The first evaluation generally kicks off the first snapshot
        /// </summary>
        protected async Task ProjectRuleBlock_ChangedAsync(IProjectVersionedValue<ValueTuple<IProjectSubscriptionUpdate, IProjectCapabilitiesSnapshot>> projectSnapshot)
        {
            if (projectSnapshot.Value.Item1.CurrentState.TryGetValue(ProjectDebugger.SchemaName, out IProjectRuleSnapshot ruleSnapshot))
            {
                ruleSnapshot.Properties.TryGetValue(ProjectDebugger.ActiveDebugProfileProperty, out string activeProfile);
                ILaunchSettings snapshot = CurrentSnapshot;
                if (snapshot == null || !LaunchProfile.IsSameProfileName(activeProfile, snapshot.ActiveProfile?.Name))
                {
                    // Updates need to be sequenced
                    await _sequentialTaskQueue.ExecuteTask(async () =>
                    {
                        using (ProjectCapabilitiesContext.CreateIsolatedContext(_commonProjectServices.Project, projectSnapshot.Value.Item2))
                        {
                            await UpdateActiveProfileInSnapshotAsync(activeProfile);
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Called when the active profile has changed. If there is a current snapshot it just updates that. Otherwise, it creates
        /// a new snapshot
        /// </summary>
        protected async Task UpdateActiveProfileInSnapshotAsync(string updatedActiveProfileName)
        {
            ILaunchSettings snapshot = CurrentSnapshot;
            if (snapshot == null || await SettingsFileHasChangedAsync())
            {
                await UpdateProfilesAsync(updatedActiveProfileName);
                return;
            }

            var newSnapshot = new LaunchSettings(snapshot.Profiles, snapshot.GlobalSettings, updatedActiveProfileName, GetNextVersion());
            FinishUpdate(newSnapshot);
        }

        /// <summary>
        /// Does the processing to update the profiles when changes have been made to either the file or the active profile name.
        /// When merging with the disk, it needs to honor in-memory only profiles that may have been programmatically added. If
        /// a profile on disk has the same name as an in-memory profile, the one on disk wins. It tries to add the in-memory profiles
        /// in the same order they appeared prior to the disk change.
        /// </summary>
        protected async Task UpdateProfilesAsync(string? updatedActiveProfileName)
        {
            try
            {
                // If the name of the new active profile wasn't provided we'll continue to use the
                // current one.
                if (updatedActiveProfileName == null)
                {
                    ProjectDebugger props = await _commonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync();
                    if (await props.ActiveDebugProfile.GetValueAsync() is IEnumValue activeProfileVal)
                    {
                        updatedActiveProfileName = activeProfileVal.Name;
                    }
                }

                LaunchSettingsData launchSettingData = await GetLaunchSettingsAsync();

                Assumes.NotNull(launchSettingData.Profiles);

                // If there are no profiles, we will add a default profile to run the project. W/o it our debugger
                // won't be called on F5 and the user will see a poor error message
                if (launchSettingData.Profiles.Count == 0)
                {
                    ILaunchProfile? defaultLaunchProfile = _defaultLaunchProfile.Value;
                    if (defaultLaunchProfile is not null)
                        launchSettingData.Profiles.Add(LaunchProfileData.FromILaunchProfile(defaultLaunchProfile));
                }

                // If we have a previous snapshot merge in in-memory profiles
                ILaunchSettings prevSnapshot = CurrentSnapshot;
                if (prevSnapshot != null)
                {
                    MergeExistingInMemoryProfiles(launchSettingData, prevSnapshot);
                    MergeExistingInMemoryGlobalSettings(launchSettingData, prevSnapshot);
                }

                var newSnapshot = new LaunchSettings(launchSettingData, updatedActiveProfileName, GetNextVersion());

                FinishUpdate(newSnapshot);
            }
            catch (Exception ex)
            {
                // Errors are added as error list entries. We don't want to throw out of here
                // However, if we have never created a snapshot it means there is some error in the file and we want
                // to have the user see that, so we add a dummy profile which will bind to an existing debugger which will
                // display the error when run
                if (CurrentSnapshot == null)
                {
                    var errorProfile = new LaunchProfile
                    {
                        Name = Resources.NoActionProfileName,
                        CommandName = ErrorProfileCommandName,
                        DoNotPersist = true,
                        OtherSettings = ImmutableStringDictionary<object>.EmptyOrdinal.Add("ErrorString", ex.Message)
                    };
                    var snapshot = new LaunchSettings(new[] { errorProfile }, null, errorProfile.Name, GetNextVersion());
                    FinishUpdate(snapshot);
                }
            }
        }

        /// <summary>
        /// Re-applies in-memory profiles to the newly created snapshot. Note that we don't want to merge in the error
        /// profile
        /// </summary>
        protected static void MergeExistingInMemoryProfiles(LaunchSettingsData newSnapshot, ILaunchSettings prevSnapshot)
        {
            for (int i = 0; i < prevSnapshot.Profiles.Count; i++)
            {
                ILaunchProfile profile = prevSnapshot.Profiles[i];
                if (profile.IsInMemoryObject() && !string.Equals(profile.CommandName, ErrorProfileCommandName))
                {
                    Assumes.NotNull(newSnapshot.Profiles);

                    // Does it already have one with this name?
                    if (newSnapshot.Profiles.FirstOrDefault((p1, p2) => LaunchProfile.IsSameProfileName(p1.Name, p2.Name), profile) == null)
                    {
                        // Create a new one from the existing in-memory profile and insert it in the same location, or the end if it
                        // is beyond the end of the list
                        if (i > newSnapshot.Profiles.Count)
                        {
                            newSnapshot.Profiles.Add(LaunchProfileData.FromILaunchProfile(profile));
                        }
                        else
                        {
                            newSnapshot.Profiles.Insert(i, LaunchProfileData.FromILaunchProfile(profile));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Re-applies in-memory global options to the newly created snapshot
        /// </summary>
        protected static void MergeExistingInMemoryGlobalSettings(LaunchSettingsData newSnapshot, ILaunchSettings prevSnapshot)
        {
            if (prevSnapshot.GlobalSettings != null)
            {
                foreach ((string key, object value) in prevSnapshot.GlobalSettings)
                {
                    if (value.IsInMemoryObject())
                    {
                        if (newSnapshot.OtherSettings == null)
                        {
                            newSnapshot.OtherSettings = new Dictionary<string, object>
                            {
                                [key] = value
                            };
                        }
                        else if (!newSnapshot.OtherSettings.TryGetValue(key, out _))
                        {
                            newSnapshot.OtherSettings[key] = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true of the file has changed since we last read it. Note that it returns true if the file
        /// does not exist
        /// </summary>
        protected async Task<bool> SettingsFileHasChangedAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync();

            if (_fileSystem.TryGetLastFileWriteTimeUtc(fileName, out DateTime? writeTime))
            {
                return writeTime != LastSettingsFileSyncTimeUtc;
            }

            return true;
        }

        /// <summary>
        /// Helper function to set the new snapshot and post the changes to consumers.
        /// </summary>
        protected void FinishUpdate(ILaunchSettings newSnapshot)
        {
            CurrentSnapshot = newSnapshot;

            // Suppress the project execution context so the data flow doesn't accidentally
            // capture it. This prevents us from leaking any active project system locks into
            // the data flow, which can lead to asserts, failures, and crashes later if it
            // causes code to execute that doesn't expect to run under a lock. Also, if a
            // consumer of the data flow needs a lock they should be acquiring it themself
            // rather than depending on inheriting it from the data flow.
            using (_commonProjectServices.ThreadingService.SuppressProjectExecutionContext())
            {
                _broadcastBlock?.Post(newSnapshot);
            }
        }

        /// <summary>
        /// Creates the initial set of settings based on the file on disk
        /// </summary>
        protected async Task<LaunchSettingsData> GetLaunchSettingsAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync();

            LaunchSettingsData settings = _fileSystem.FileExists(fileName)
                ? await ReadSettingsFileFromDiskAsync()
                : new LaunchSettingsData();

            // Make sure there is at least an empty profile list
            settings.Profiles ??= new List<LaunchProfileData>();

            return settings;
        }

        /// <summary>
        /// Reads the data from the launch settings file and returns it in a dictionary of settings section to object. Adds n error list entries
        /// and throws if an exception occurs
        /// </summary>
        protected async Task<LaunchSettingsData> ReadSettingsFileFromDiskAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync();

            string jsonString = _fileSystem.ReadAllText(fileName);

            // Since the sections in the settings file are extensible we iterate through each one and have the appropriate provider
            // serialize their section. Unfortunately, this means the data is string to object which is messy to deal with
            var launchSettingsData = new LaunchSettingsData() { OtherSettings = new Dictionary<string, object>(StringComparers.LaunchProfileProperties) };
            var jsonObject = JObject.Parse(jsonString);
            foreach ((string key, JToken jToken) in jsonObject)
            {
                if (key.Equals(ProfilesSectionName, StringComparisons.LaunchSettingsPropertyNames) && jToken is JObject jObject)
                {
                    Dictionary<string, LaunchProfileData> profiles = LaunchProfileData.DeserializeProfiles(jObject);
                    launchSettingsData.Profiles = FixUpProfilesAndLogErrors(profiles);
                }
                else
                {
                    // Find the matching json serialization handler for this section
                    Lazy<ILaunchSettingsSerializationProvider, IJsonSection> handler = JsonSerializationProviders.FirstOrDefault(sp => string.Equals(sp.Metadata.JsonSection, key));
                    if (handler != null)
                    {
                        object sectionObject = JsonConvert.DeserializeObject(jToken.ToString(), handler.Metadata.SerializationType);
                        launchSettingsData.OtherSettings.Add(key, sectionObject);
                    }
                    else
                    {
                        // We still need to remember settings for which we don't have an extensibility component installed. For this we
                        // just keep the jObject which can be serialized back out when the file is written.
                        launchSettingsData.OtherSettings.Add(key, jToken);
                    }
                }
            }

            // Remember the time we are sync'd to
            LastSettingsFileSyncTimeUtc = _fileSystem.GetLastFileWriteTimeOrMinValueUtc(fileName);
            return launchSettingsData;
        }

        public Task<string> GetLaunchSettingsFilePathAsync()
        {
            return _launchSettingsFilePath.GetValueAsync();
        }

        /// <summary>
        /// Does a quick validation to make sure at least a name is present in each profile. Removes bad ones and
        /// logs errors. Returns the resultant profiles as a list
        /// </summary>
        private static List<LaunchProfileData>? FixUpProfilesAndLogErrors(Dictionary<string, LaunchProfileData> profilesData)
        {
            if (profilesData == null)
            {
                return null;
            }

            var validProfiles = new List<LaunchProfileData>();
            foreach ((string name, LaunchProfileData launchProfileData) in profilesData)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    // The name is if the profile is set to the value key
                    launchProfileData.Name = name;
                    validProfiles.Add(launchProfileData);
                }
            }

            return validProfiles;
        }

        /// <summary>
        /// Saves the launch settings to the launch settings file. Adds an error string and throws if an exception. Note
        /// that the caller is responsible for checking out the file
        /// </summary>
        protected async Task SaveSettingsToDiskAsync(ILaunchSettings newSettings)
        {
            Dictionary<string, object> serializationData = GetSettingsToSerialize(newSettings);
            string fileName = await GetLaunchSettingsFilePathAsync();

            try
            {
                await EnsureSettingsFolderAsync();

                // We don't want to write null values. We want to keep the file as small as possible
                var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                string jsonString = JsonConvert.SerializeObject(serializationData, Formatting.Indented, settings);

                IgnoreFileChanges = true;
                _fileSystem.WriteAllText(fileName, jsonString);

                // Update the last write time
                LastSettingsFileSyncTimeUtc = _fileSystem.GetLastFileWriteTimeOrMinValueUtc(fileName);
            }
            finally
            {
                IgnoreFileChanges = false;
            }
        }

        /// <summary>
        /// Gets the serialization object for the set of profiles and custom settings. It filters out built in profiles that get added to
        /// wire up the debugger infrastructure (NoAction profiles). Returns a dictionary of the elements to serialize.
        /// Removes in-memory profiles and global objects
        /// </summary>
        protected static Dictionary<string, object> GetSettingsToSerialize(ILaunchSettings curSettings)
        {
            var profileData = new Dictionary<string, Dictionary<string, object>>(StringComparers.LaunchProfileNames);
            foreach (ILaunchProfile profile in curSettings.Profiles)
            {
                if (ProfileShouldBePersisted(profile))
                {
                    Assumes.NotNull(profile.Name);
                    profileData.Add(profile.Name, LaunchProfileData.ToSerializableForm(profile));
                }
            }

            var dataToSave = new Dictionary<string, object>(StringComparers.LaunchProfileProperties);

            foreach ((string key, object value) in curSettings.GlobalSettings)
            {
                if (!value.IsInMemoryObject())
                {
                    dataToSave.Add(key, value);
                }
            }

            if (profileData.Count > 0)
            {
                dataToSave.Add(ProfilesSectionName, profileData);
            }

            return dataToSave;
        }

        /// <summary>
        /// Helper returns true if this is a profile which should be persisted.
        /// Filters out <see cref="IPersistOption.DoNotPersist"/> profiles.
        /// </summary>
        private static bool ProfileShouldBePersisted(ILaunchProfile profile)
        {
            return !profile.IsInMemoryObject();
        }

        /// <summary>
        /// Helper to check out the debugsettings.json file
        /// </summary>
        protected async Task CheckoutSettingsFileAsync()
        {
            Lazy<ISourceCodeControlIntegration, IOrderPrecedenceMetadataView>? sourceControlIntegration = SourceControlIntegrations.FirstOrDefault();
            if (sourceControlIntegration?.Value != null)
            {
                string fileName = await GetLaunchSettingsFilePathAsync();

                await sourceControlIntegration.Value.CanChangeProjectFilesAsync(new[] { fileName });
            }
        }

        /// <summary>
        /// Handler for when the Launch settings file changes. Actually, we watch the project root so any
        /// file with the name LaunchSettings.json. We don't need to special case because, if a file with this name
        /// changes we will only check if the one we cared about was modified.
        /// </summary>
        private void LaunchSettingsFile_Changed(object sender, FileSystemEventArgs e)
        {
            _projectFaultHandler.Forget(HandleLaunchSettingsFileChangedAsync(), _project);
        }

        protected Task HandleLaunchSettingsFileChangedAsync()
        {
            if (!IgnoreFileChanges)
            {
#pragma warning disable CS0618  // We're in a synchronous callback
                string fileName = LaunchSettingsFile;
#pragma warning restore CS0618

                // Only do something if the file is truly different than what we synced. Here, we want to
                // throttle.
                if (_fileSystem.GetLastFileWriteTimeOrMinValueUtc(fileName) != LastSettingsFileSyncTimeUtc)
                {
                    Assumes.NotNull(FileChangeScheduler);

                    return FileChangeScheduler.ScheduleAsyncTask(token =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            return Task.CompletedTask;
                        }

                        // Updates need to be sequenced
                        return _sequentialTaskQueue.ExecuteTask(() => UpdateProfilesAsync(null));
                    }).Task;
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Makes sure the settings folder exists on disk. Doesn't add the folder to
        /// the project.
        /// </summary>
        protected async Task EnsureSettingsFolderAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync();

            string parentPath = Path.GetDirectoryName(fileName);

            _fileSystem.CreateDirectory(parentPath);
        }

        /// <summary>
        /// Cleans up our watcher on the debugsettings.Json file
        /// </summary>
        private void CleanupFileWatcher()
        {
            if (FileWatcher != null)
            {
                FileWatcher.Dispose();
                FileWatcher = null;
            }
        }

        /// <summary>
        /// Sets up a file system watcher to look for changes to the launchsettings.json file. It watches at the root of the
        /// project otherwise we force the project to have a properties folder.
        /// </summary>
        private void WatchLaunchSettingsFile()
        {
            if (FileWatcher == null)
            {
                FileChangeScheduler?.Dispose();

                Assumes.Present(_projectServices.ProjectAsynchronousTasks);

                // Create our scheduler for processing file changes
                FileChangeScheduler = new TaskDelayScheduler(FileChangeProcessingDelay, _commonProjectServices.ThreadingService,
                    _projectServices.ProjectAsynchronousTasks.UnloadCancellationToken);

                try
                {
                    FileWatcher = new SimpleFileWatcher(Path.GetDirectoryName(_commonProjectServices.Project.FullPath),
                                                        true,
                                                        NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
                                                        LaunchSettingsFilename,
                                                        LaunchSettingsFile_Changed,
                                                        LaunchSettingsFile_Changed);
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException)
                {
                    // If the project folder is no longer available this will throw, which can happen during branch switching
                }
            }
        }

        /// <summary>
        /// Need to make sure we cleanup the dataflow links and file watcher
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupFileWatcher();
                if (FileChangeScheduler != null)
                {
                    FileChangeScheduler.Dispose();
                    FileChangeScheduler = null;
                }

                _sequentialTaskQueue.Dispose();

                if (_broadcastBlock != null)
                {
                    _broadcastBlock.Complete();
                    _broadcastBlock = null;
                }

                if (_projectRuleSubscriptionLink != null)
                {
                    _projectRuleSubscriptionLink.Dispose();
                    _projectRuleSubscriptionLink = null;
                }

                _firstSnapshotCompletionSource.TrySetCanceled();
            }
        }

        /// <summary>
        /// Replaces the current set of profiles with the contents of profiles. If changes were
        /// made, the file will be checked out and saved. Note it ignores the value of the active profile
        /// as this setting is controlled by a user property.
        /// </summary>
        public Task UpdateAndSaveSettingsAsync(ILaunchSettings newSettings)
        {
            // Updates need to be sequenced. Do not call this version from within an ExecuteTask as it
            // will deadlock
            return _sequentialTaskQueue.ExecuteTask(() => UpdateAndSaveSettingsInternalAsync(newSettings));
        }

        /// <summary>
        /// Replaces the current set of profiles with the contents of profiles. If changes were
        /// made, the file will be checked out and saved. Note it ignores the value of the active profile
        /// as this setting is controlled by a user property.
        /// </summary>
        protected async Task UpdateAndSaveSettingsInternalAsync(ILaunchSettings newSettings, bool persistToDisk = true)
        {
            if (persistToDisk)
            {
                await CheckoutSettingsFileAsync();
            }

            // Make sure the profiles are copied. We don't want them to mutate.
            string? activeProfileName = ActiveProfile?.Name;

            ILaunchSettings newSnapshot = new LaunchSettings(newSettings.Profiles, newSettings.GlobalSettings, activeProfileName, GetNextVersion());
            if (persistToDisk)
            {
                await SaveSettingsToDiskAsync(newSettings);
            }

            FinishUpdate(newSnapshot);
        }

        /// <summary>
        /// This function blocks until a snapshot is available. It will return null if the timeout occurs
        /// prior to the snapshot is available. The wait operation will be cancelled if this object is disposed.
        /// </summary>
        public async Task<ILaunchSettings?> WaitForFirstSnapshot(int timeout)
        {
            if (CurrentSnapshot != null)
            {
                return CurrentSnapshot;
            }

            if (await _firstSnapshotCompletionSource.Task.TryWaitForCompleteOrTimeoutAsync(timeout))
            {
                Assumes.NotNull(CurrentSnapshot);
            }

            return CurrentSnapshot;
        }

        /// <summary>
        /// Adds the given profile to the list and saves to disk. If a profile with the same
        /// name exists (case sensitive), it will be replaced with the new profile. If addToFront is
        /// true the profile will be the first one in the list. This is useful since quite often callers want
        /// their just added profile to be listed first in the start menu. If addToFront is false but there is
        /// an existing profile, the new one will be inserted at the same location rather than at the end.
        /// </summary>
        public Task AddOrUpdateProfileAsync(ILaunchProfile profile, bool addToFront)
        {
            // Updates need to be sequenced
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
                ILaunchProfile? existingProfile = null;
                int insertionIndex = 0;
                foreach (ILaunchProfile p in currentSettings.Profiles)
                {
                    if (LaunchProfile.IsSameProfileName(p.Name, profile.Name))
                    {
                        existingProfile = p;
                        break;
                    }
                    insertionIndex++;
                }

                ImmutableList<ILaunchProfile> profiles;
                if (existingProfile != null)
                {
                    profiles = currentSettings.Profiles.Remove(existingProfile);
                }
                else
                {
                    profiles = currentSettings.Profiles;
                }

                if (addToFront)
                {
                    profiles = profiles.Insert(0, new LaunchProfile(profile));
                }
                else
                {
                    // Insertion index will be set to the current count (end of list) if an existing item was not found otherwise
                    // it will point to where the previous one was found
                    profiles = profiles.Insert(insertionIndex, new LaunchProfile(profile));
                }

                // If the new profile is in-memory only, we don't want to touch the disk unless it replaces an existing disk based
                // profile
                bool saveToDisk = !profile.IsInMemoryObject() || (existingProfile?.IsInMemoryObject() == false);

                var newSnapshot = new LaunchSettings(profiles, currentSettings?.GlobalSettings, currentSettings?.ActiveProfile?.Name, GetNextVersion());
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
            });
        }

        /// <summary>
        /// Removes the specified profile from the list and saves to disk.
        /// </summary>
        public Task RemoveProfileAsync(string profileName)
        {
            // Updates need to be sequenced
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
                ILaunchProfile existingProfile = currentSettings.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, profileName));
                if (existingProfile != null)
                {
                    ImmutableList<ILaunchProfile> profiles = currentSettings.Profiles.Remove(existingProfile);

                    // If the new profile is in-memory only, we don't want to touch the disk
                    bool saveToDisk = !existingProfile.IsInMemoryObject();
                    var newSnapshot = new LaunchSettings(profiles, currentSettings.GlobalSettings, currentSettings.ActiveProfile?.Name, GetNextVersion());
                    await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
                }
            });
        }

        public Task<bool> TryUpdateProfileAsync(string profileName, Action<IWritableLaunchProfile> updateAction)
        {
            // Updates need to be sequenced
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
                ILaunchProfile? existingProfile = null;
                int insertionIndex = 0;
                foreach (ILaunchProfile p in currentSettings.Profiles)
                {
                    if (LaunchProfile.IsSameProfileName(p.Name, profileName))
                    {
                        existingProfile = p;
                        break;
                    }
                    insertionIndex++;
                }

                if (existingProfile is null)
                {
                    return false;
                }

                var writableProfile = new WritableLaunchProfile(existingProfile);
                updateAction(writableProfile);
                ILaunchProfile updatedProfile = writableProfile.ToLaunchProfile();

                ImmutableList<ILaunchProfile> profiles = currentSettings.Profiles.Remove(existingProfile);

                // Insertion index will point to where the previous one was found
                profiles = profiles.Insert(insertionIndex, updatedProfile);

                // If the updated profile is in-memory only, we don't want to touch the disk unless it replaces an existing disk based
                // profile
                bool saveToDisk = !updatedProfile.IsInMemoryObject() || (existingProfile?.IsInMemoryObject() == false);

                var newSnapshot = new LaunchSettings(profiles, currentSettings?.GlobalSettings, currentSettings?.ActiveProfile?.Name, GetNextVersion());
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);

                return true;
            });
        }

        /// <summary>
        /// Adds or updates the global settings represented by settingName. Saves the
        /// updated settings to disk. Note that the settings object must be serializable.
        /// </summary>
        public Task AddOrUpdateGlobalSettingAsync(string settingName, object settingContent)
        {
            // Updates need to be sequenced
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
                ImmutableDictionary<string, object> globalSettings = ImmutableStringDictionary<object>.EmptyOrdinal;
                if (currentSettings.GlobalSettings.TryGetValue(settingName, out object? currentValue))
                {
                    globalSettings = currentSettings.GlobalSettings.Remove(settingName);
                }
                else
                {
                    globalSettings = currentSettings.GlobalSettings;
                }

                bool saveToDisk = !settingContent.IsInMemoryObject() || (currentValue?.IsInMemoryObject() == false);

                var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings.Add(settingName, settingContent), currentSettings.ActiveProfile?.Name, GetNextVersion());
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
            });
        }

        /// <summary>
        /// Removes the specified global setting and saves the settings to disk
        /// </summary>
        public Task RemoveGlobalSettingAsync(string settingName)
        {
            // Updates need to be sequenced
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
                if (currentSettings.GlobalSettings.TryGetValue(settingName, out object? currentValue))
                {
                    bool saveToDisk = !currentValue.IsInMemoryObject();
                    ImmutableDictionary<string, object> globalSettings = currentSettings.GlobalSettings.Remove(settingName);
                    var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings, currentSettings.ActiveProfile?.Name, GetNextVersion());
                    await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
                }
            });
        }

        /// <summary>
        /// Retrieves and updates the global settings as a single operation and saves the
        /// settings to disk.
        /// </summary>
        public Task UpdateGlobalSettingsAsync(Func<ImmutableDictionary<string, object>, ImmutableDictionary<string, object?>> updateFunction)
        {
            return _sequentialTaskQueue.ExecuteTask(async () =>
            {
                ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
                ImmutableDictionary<string, object> globalSettings = currentSettings.GlobalSettings;

                ImmutableDictionary<string, object?> updatesToGlobalSettings = updateFunction(globalSettings);

                bool saveToDisk = false;

                foreach ((string updatedSettingName, object? updatedSettingValue) in updatesToGlobalSettings)
                {
                    globalSettings.TryGetValue(updatedSettingName, out object? currentValue);

                    bool originalValueWasSavedToDisk = currentValue?.IsInMemoryObject() == false;

                    globalSettings = globalSettings.Remove(updatedSettingName);
                    if (updatedSettingValue is not null)
                    {
                        globalSettings = globalSettings.Add(updatedSettingName, updatedSettingValue);
                    }

                    bool saveThisSettingToDisk = (updatedSettingValue is not null && !updatedSettingValue.IsInMemoryObject())
                        || originalValueWasSavedToDisk;

                    saveToDisk = saveToDisk || saveThisSettingToDisk;
                }

                var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings, currentSettings.ActiveProfile?.Name, GetNextVersion());
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
            });
        }

        /// <summary>
        /// Helper retrieves the current snapshot (waiting up to 5s) and if there were errors in the launchsettings.json file
        /// or there isn't a snapshot, it throws an error. There should always be a snapshot of some kind returned
        /// </summary>
        public async Task<ILaunchSettings> GetSnapshotThrowIfErrorsAsync()
        {
            ILaunchSettings? currentSettings = await WaitForFirstSnapshot(WaitForFirstSnapshotDelayMillis);
            if (currentSettings == null || (currentSettings.Profiles.Count == 1 && string.Equals(currentSettings.Profiles[0].CommandName, ErrorProfileCommandName, StringComparisons.LaunchProfileCommandNames)))
            {
                string fileName = await GetLaunchSettingsFilePathAsync();

                throw new Exception(string.Format(Resources.JsonErrorNeedToBeCorrected, fileName));
            }

            return currentSettings;
        }

        /// <summary>
        /// Sets the active profile. This just sets the property it does not validate that the setting matches an
        /// existing profile
        /// </summary>
        public async Task SetActiveProfileAsync(string profileName)
        {
            ProjectDebugger props = await _commonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync();
            await props.ActiveDebugProfile.SetValueAsync(profileName);
        }

        internal async Task<string> GetLaunchSettingsFilePathNoCacheAsync()
        {
            // NOTE: To reduce behavior changes, we currently cache the folder that we get from the AppDesignerSpecialFileProvider,
            // even though it can change over the lifetime of the project. We should fix this and convert to using dataflow
            // see: https://github.com/dotnet/project-system/issues/2316.

            string? folder = null;
            if (_appDesignerSpecialFileProvider.Value != null)
                folder = await _appDesignerSpecialFileProvider.Value.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.FullPath);

            if (folder == null)  // AppDesigner capability not present, or the project has set AppDesignerFolder to empty
                folder = Path.GetDirectoryName(_commonProjectServices.Project.FullPath);

            return Path.Combine(folder, LaunchSettingsFilename);
        }

        private long GetNextVersion() => _nextVersion++;

        /// <summary>
        /// Protected method for testing purposes.
        /// </summary>
        protected void SetNextVersion(long nextVersion) => _nextVersion = nextVersion;
    }
}
