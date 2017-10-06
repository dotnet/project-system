// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    internal class LaunchSettingsProvider : OnceInitializedOnceDisposed, ILaunchSettingsProvider2
    {
        private readonly ActiveConfiguredProject<AppDesignerFolderSpecialFileProvider> _appDesignerSpecialFileProvider;
        private readonly AsyncLazy<string> _launchSettingsFilePath;

        [ImportingConstructor]
        public LaunchSettingsProvider(UnconfiguredProject unconfiguredProject, IUnconfiguredProjectServices projectServices,
                                      IFileSystem fileSystem, IUnconfiguredProjectCommonServices commonProjectServices,
                                      IActiveConfiguredProjectSubscriptionService projectSubscriptionService,
                                      ActiveConfiguredProject<AppDesignerFolderSpecialFileProvider> appDesignerSpecialFileProvider)
        {
            ProjectServices = projectServices;
            FileManager = fileSystem;
            CommonProjectServices = commonProjectServices;
            JsonSerializationProviders = new OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst,
                                                                                                                    unconfiguredProject);
            SourceControlIntegrations = new OrderPrecedenceImportCollection<ISourceCodeControlIntegration>(projectCapabilityCheckProvider: unconfiguredProject);

            ProjectSubscriptionService = projectSubscriptionService;
            _appDesignerSpecialFileProvider = appDesignerSpecialFileProvider;
            _launchSettingsFilePath = new AsyncLazy<string>(GetLaunchSettingsFilePathNoCacheAsync, commonProjectServices.ThreadingService.JoinableTaskFactory);
        }

        // TODO: Add error list support. Tracked by https://github.com/dotnet/roslyn-project-system/issues/424
        //protected IProjectErrorManager ProjectErrorManager { get; }

        private IUnconfiguredProjectServices ProjectServices { get; }
        private IUnconfiguredProjectCommonServices CommonProjectServices { get; }
        private IActiveConfiguredProjectSubscriptionService ProjectSubscriptionService { get; }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> JsonSerializationProviders { get; set; }

        [ImportMany]
        private OrderPrecedenceImportCollection<ISourceCodeControlIntegration> SourceControlIntegrations { get; set; }

        // The source for our dataflow
        private IReceivableSourceBlock<ILaunchSettings> _changedSourceBlock;
        protected BroadcastBlock<ILaunchSettings> _broadcastBlock;

        protected IFileSystem FileManager { get; set; }

        // Used to track our errors so we can flush them later
        public const string ErrorOwnerString = nameof(LaunchSettingsProvider);

        public const string LaunchSettingsFilename = @"launchSettings.json";
        public const string ProfilesSectionName = "profiles";

        // Command that means run this project
        public const string RunProjectCommandName = "Project";

        //  Command that means run an executable
        public const string RunExecutableCommandName = "Executable";

        // These are used internally to loop in debuggers to handle F5 when there are errors in 
        // the launch settings file or when there are no profiles specified (like class libraries)
        public const string ErrorProfileCommandName = "ErrorProfile";

        protected SimpleFileWatcher FileWatcher { get; set; }

        // When we are saveing the file we set this to minimize noise from the file change
        protected bool IgnoreFileChanges { get; set; }

        protected TimeSpan FileChangeProcessingDelay = TimeSpan.FromMilliseconds(500);

        public ITaskDelayScheduler FileChangeScheduler { get; protected set; }

        // Tracks when we last read or wrote to the file. Prevents picking up needless changes
        protected DateTime LastSettingsFileSyncTime { get; set; }

        protected int WaitForFirstSnapshotDelay = 5000; // 5 seconds

        private TaskCompletionSource<bool> _firstSnapshotCompletionSource = new TaskCompletionSource<bool>();

        protected IDisposable ProjectRuleSubscriptionLink { get; set; }

        private SequencialTaskExecutor _sequentialTaskQueue = new SequencialTaskExecutor();

        [Obsolete("Use GetLaunchSettingsFilePathAsync instead.")]
        public string LaunchSettingsFile
        {
            get
            {
                return CommonProjectServices.ThreadingService.ExecuteSynchronously(() => {
                    return GetLaunchSettingsFilePathAsync();
                });
            }
        }

        /// <summary>
        /// Returns the active profile. Looks up the value of the ActiveProfile property. If the value doesn't match the
        /// any of the profiles, the first one is returned
        /// </summary>
        public ILaunchProfile ActiveProfile
        {
            get
            {
                var snapshot = CurrentSnapshot;
                return snapshot?.ActiveProfile;
            }
        }
        /// <summary>
        /// Link to this source block to be notified when the snapshot is changed.
        /// </summary>
        public IReceivableSourceBlock<ILaunchSettings> SourceBlock
        {
            get
            {
                EnsureInitialized();
                return _changedSourceBlock;
            }
        }

        /// <summary>
        /// IOebugProfileProvider
        /// Access to the current set of profile information
        /// </summary>
        private ILaunchSettings _currentSnapshot;
        public ILaunchSettings CurrentSnapshot
        {
            get
            {
                EnsureInitialized();
                return _currentSnapshot;
            }
            protected set
            {
                // If this is the first snapshot, complete the taskCompletionSource
                if (_currentSnapshot == null)
                {
                    _firstSnapshotCompletionSource.TrySetResult(true);
                }
                _currentSnapshot = value;
            }
        }

        /// <summary>
        /// The DebugProfileProvider sinks 2 sets of information
        /// 1, Changes to the launchsettings.json file on disk
        /// 2. Changes to the ActiveDebugProfile property in the .user file
        /// </summary>
        protected override void Initialize()
        {
            // Create our broadcast block for subscribers to get new ILaunchProfiles Information
            _broadcastBlock = new BroadcastBlock<ILaunchSettings>(s => s);
            _changedSourceBlock = _broadcastBlock.SafePublicize();


            // Subscribe to changes to the broadcast block using the idle scheduler. This should filter out a lot of the intermediates 
            // states that files can be in.
            if (ProjectSubscriptionService != null)
            {
                // The use of AsyncLazy with dataflow can allow state stored in the execution context to leak through. The downstream affect is 
                // calls to say, get properties, may fail. To avoid this, we capture the execution context here, and it will be reapplied when
                // we get new subscription data from the dataflow. 
                var projectChangesBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(
                            DataflowUtilities.CaptureAndApplyExecutionContext<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ProjectRuleBlock_ChangedAsync));

                ProjectRuleSubscriptionLink = ProjectSubscriptionService.ProjectRuleSource.SourceBlock.LinkTo(
                    projectChangesBlock,
                    ruleNames: ProjectDebugger.SchemaName,
                    linkOptions: new DataflowLinkOptions { PropagateCompletion = true });
            }

            // Make sure we are watching the file at this point
            WatchLaunchSettingsFile();
        }

        /// <summary>
        /// Handles changes to the ProjectDebugger properties. Gets the active profile and generates a launch settings update if it
        /// has changed. The first evaluation generally kicks off the first snapshot
        /// </summary>
        protected async Task ProjectRuleBlock_ChangedAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> projectSubscriptionUpdate)
        {
            if (projectSubscriptionUpdate.Value.CurrentState.TryGetValue(ProjectDebugger.SchemaName, out IProjectRuleSnapshot ruleSnapshot))
            {
                ruleSnapshot.Properties.TryGetValue(ProjectDebugger.ActiveDebugProfileProperty, out string activeProfile);
                var snapshot = CurrentSnapshot;
                if (snapshot == null || !LaunchProfile.IsSameProfileName(activeProfile, snapshot.ActiveProfile?.Name))
                {
                    // Updates need to be sequenced
                    await _sequentialTaskQueue.ExecuteTask(async () =>
                                    {
                                        await UpdateActiveProfileInSnapshotAsync(activeProfile).ConfigureAwait(false);
                                    }).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Called when the active profile has changed. If there is a current snapshot it just updates that. Otherwwise, it creates
        /// a new snapshot
        /// </summary>
        protected async Task UpdateActiveProfileInSnapshotAsync(string activeProfile)
        {
            var snapshot = CurrentSnapshot;
            if (snapshot == null || await SettingsFileHasChangedAsync().ConfigureAwait(false))
            {
                await UpdateProfilesAsync(activeProfile).ConfigureAwait(false);
                return;
            }

            var newSnapshot = new LaunchSettings(snapshot.Profiles, snapshot.GlobalSettings, activeProfile);
            FinishUpdate(newSnapshot);
        }

        /// <summary>
        /// Does the processing to update the profiles when changes have been made to either the file or the active profile. 
        /// When merging with the disk, it needs to honor in-memory only profiles that may have been programmatically added. If 
        /// a profile on disk has the same name as an in-memory profile, the one on disk wins. It tries to add the in-memory profiles
        /// in the same order they appeared prior to the disk change.
        /// </summary>
        protected async Task UpdateProfilesAsync(string activeProfile)
        {
            try
            {
                // If no active profile specified, try to get one
                if (activeProfile == null)
                {
                    var props = await CommonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync().ConfigureAwait(true);
                    if (await props.ActiveDebugProfile.GetValueAsync().ConfigureAwait(true) is IEnumValue activeProfileVal)
                    {
                        activeProfile = activeProfileVal.Name;
                    }
                }

                var launchSettingData = await GetLaunchSettingsAsync().ConfigureAwait(false);

                // If there are no profiles, we will add a default profile to run the prroject. W/o it our debugger
                // won't be called on F5 and the user will see a poor error message
                if (launchSettingData.Profiles.Count == 0)
                {
                    launchSettingData.Profiles.Add(new LaunchProfileData() { Name = Path.GetFileNameWithoutExtension(CommonProjectServices.Project.FullPath), CommandName = RunProjectCommandName });
                }

                // If we have a previous snapshot merge in in-memory profiles
                var prevSnapshot = CurrentSnapshot;
                if(prevSnapshot != null)
                {
                    MergeExistingInMemoryProfiles(launchSettingData, prevSnapshot);
                    MergeExistingInMemoryGlobalSettings(launchSettingData, prevSnapshot);
                }

                var newSnapshot = new LaunchSettings(launchSettingData, activeProfile);

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
                    var errorProfile = new LaunchProfile() { Name = Resources.NoActionProfileName, CommandName = ErrorProfileCommandName, DoNotPersist = true};
                    errorProfile.OtherSettings = ImmutableDictionary<string, object>.Empty.Add("ErrorString", ex.Message);
                    var snapshot = new LaunchSettings(new List<ILaunchProfile>() { errorProfile }, null, errorProfile.Name);
                    FinishUpdate(snapshot);
                }
            }
        }

        /// <summary>
        /// Re-applies in-memory profiles to the newly created snapshot
        /// </summary>
        protected void MergeExistingInMemoryProfiles(LaunchSettingsData newSnapshot, ILaunchSettings prevSnapshot)
        {
            for (int i =0; i < prevSnapshot.Profiles.Count; i++)
            {
                var profile = prevSnapshot.Profiles[i];
                if(profile.IsInMemoryObject())
                {
                    // Does it already have one with this name?
                    if(newSnapshot.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, profile.Name)) == null)
                    {
                        // Create a new one from the existing in-memory profile and insert it in the same location, or the end if it
                        // is beyond the end of the list
                        if(i > newSnapshot.Profiles.Count)
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
        protected void MergeExistingInMemoryGlobalSettings(LaunchSettingsData newSnapshot, ILaunchSettings prevSnapshot)
        {
            if(prevSnapshot.GlobalSettings != null)
            {
                foreach (var kvp in prevSnapshot.GlobalSettings)
                {
                    if(kvp.Value.IsInMemoryObject())
                    {
                        if(newSnapshot.OtherSettings == null)
                        {
                            newSnapshot.OtherSettings = new Dictionary<string, object> ();
                            newSnapshot.OtherSettings[kvp.Key] = kvp.Value;
                        }
                        else if(!newSnapshot.OtherSettings.TryGetValue(kvp.Key, out var existingValue))
                        {
                            newSnapshot.OtherSettings[kvp.Key] = kvp.Value;
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
            string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

            return !FileManager.FileExists(fileName) || FileManager.LastFileWriteTime(fileName) != LastSettingsFileSyncTime;
        }

        /// <summary>
        /// Helper function to set the new snapshot and post the change to consumers.
        /// </summary>
        protected void FinishUpdate(ILaunchSettings newSnapshot)
        {
            CurrentSnapshot = newSnapshot;

            if (_broadcastBlock != null)
            {
                _broadcastBlock.Post(newSnapshot);
            }
        }

        /// <summary>
        /// Gets the active profile based on the property changes
        /// </summary>
        protected string GetActiveProfile(IProjectSubscriptionUpdate projectSubscriptionUpdate)
        {
            if (projectSubscriptionUpdate.CurrentState.TryGetValue(ProjectDebugger.SchemaName, out IProjectRuleSnapshot ruleSnapshot) && 
                ruleSnapshot.Properties.TryGetValue(ProjectDebugger.ActiveDebugProfileProperty, out string activeProfile))
            {
                return activeProfile;
            }
            return null;
        }

        /// <summary>
        /// Creates the intiial set of settings based on the file on disk 
        /// </summary>
        protected async Task<LaunchSettingsData> GetLaunchSettingsAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

            LaunchSettingsData settings;
            if (FileManager.FileExists(fileName))
            {
                settings = await ReadSettingsFileFromDiskAsync().ConfigureAwait(false);
            }
            else
            {
                // Still clear errors even if no file on disk. This handles the case where there was a file with errors on
                // disk and the user deletes the file.
                ClearErrors();
                settings = new LaunchSettingsData();
            }

            // Make sure there is at least an empty profile list 
            if (settings.Profiles == null)
            {
                settings.Profiles = new List<LaunchProfileData>();
            }

            return settings;
        }

        /// <summary>
        /// Reads the data from the launch settings file and returns it in a dictionary of settings section to object. Adds n error list entries
        /// and throws if an exception occurs
        /// </summary>
        protected async Task<LaunchSettingsData> ReadSettingsFileFromDiskAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

            // Clear errors
            ClearErrors();
            try
            {
                string jsonString = FileManager.ReadAllText(fileName);

                // Since the sections in the settings file are extensible we iterate through each one and have the appropriate provider
                // serialize their section. Unfortunately, this means the data is string to object which is messy to deal with
                var launchSettingsData = new LaunchSettingsData() { OtherSettings = new Dictionary<string, object>(StringComparer.Ordinal) };
                JObject jsonObject = JObject.Parse(jsonString);
                foreach (var pair in jsonObject)
                {
                    if (pair.Key.Equals(ProfilesSectionName, StringComparison.Ordinal) && pair.Value is JObject)
                    {
                        var profiles = LaunchProfileData.DeserializeProfiles((JObject)pair.Value);
                        launchSettingsData.Profiles = FixupProfilesAndLogErrors(profiles);
                    }
                    else
                    {
                        // Find the matching json serialization handler for this section
                        var handler = JsonSerializationProviders.FirstOrDefault(sp => string.Equals(sp.Metadata.JsonSection, pair.Key));
                        if (handler != null)
                        {
                            object sectionObject = JsonConvert.DeserializeObject(pair.Value.ToString(), handler.Metadata.SerializationType);
                            launchSettingsData.OtherSettings.Add(pair.Key, sectionObject);
                        }
                        else
                        {
                            // We still need to remember settings for which we don't have an extensibility component installed. For this we
                            // just keep the jObject which can be serialized back out when the file is written.
                            launchSettingsData.OtherSettings.Add(pair.Key, pair.Value);
                        }
                    }
                }

                // Remember the time we are sync'd to
                LastSettingsFileSyncTime = FileManager.LastFileWriteTime(fileName);
                return launchSettingsData;
            }
            catch (JsonReaderException readerEx)
            {
                string err = string.Format(Resources.JsonErrorReadingLaunchSettings, readerEx.Message);
                LogError(err, fileName, readerEx.LineNumber, readerEx.LinePosition, false);
                throw;
            }
            catch (JsonException jsonEx)
            {
                string err = string.Format(Resources.JsonErrorReadingLaunchSettings, jsonEx.Message);
                LogError(err, fileName, -1, -1, false);
                throw;
            }
            catch (Exception ex)
            {
                string err = string.Format(Resources.ErrorReadingLaunchSettings, fileName, ex.Message);
                LogError(err, false);
                throw;
            }
        }

        public Task<string> GetLaunchSettingsFilePathAsync()
        {
            return _launchSettingsFilePath.GetValueAsync();
        }

        /// <summary>
        /// Does a quick validation to make sure at least a name is present in each profile. Removes bad ones and
        /// logs errors. Returns the resultant profiles as a list
        /// </summary>
        private List<LaunchProfileData> FixupProfilesAndLogErrors(Dictionary<string, LaunchProfileData> profilesData)
        {
            if (profilesData == null)
            {
                return null;
            }

            List<LaunchProfileData> validProfiles = new List<LaunchProfileData>();
            foreach (var kvp in profilesData)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    // The name is if the profile is set to the value key
                    kvp.Value.Name = kvp.Key;
                    validProfiles.Add(kvp.Value);
                }
            }

            if (validProfiles.Count < profilesData.Count)
            {
                LogError(Resources.ProfileMissingName, false);
            }

            return validProfiles;
        }

        private void LogError(string errorText, bool isWarning)
        {
            // ProjectErrorManager.AddError(ErrorOwnerString, errorText, isWarning);
        }

        private void LogError(string errorText, string filename, int line, int col, bool isWarning)
        {
            // ProjectErrorManager.AddError(ErrorOwnerString, errorText, filename, line, col, isWarning);
        }

        private void ClearErrors()
        {
            //ProjectErrorManager.ClearErrorsForOwner(ErrorOwnerString);
        }

        /// <summary>
        /// Saves the launch settings to the launch settings file. Adds an errorstring and throws if an exception. Note
        /// that the caller is responsible for checking out the file
        /// </summary>
        protected async Task SaveSettingsToDiskAsync(ILaunchSettings newSettings)
        {
            // Clear stale errors since we are saving
            ClearErrors();
            var serializationData = GetSettingsToSerialize(newSettings);
            string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

            try
            {
                await EnsureSettingsFolderAsync().ConfigureAwait(false);

                // We don't want to write null values. We want to keep the file as small as possible
                JsonSerializerSettings settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
                string jsonString = JsonConvert.SerializeObject(serializationData, Formatting.Indented, settings);

                IgnoreFileChanges = true;
                FileManager.WriteAllText(fileName, jsonString);

                // Update the last write time
                LastSettingsFileSyncTime = FileManager.LastFileWriteTime(fileName);
            }
            catch (Exception ex)
            {
                string err = string.Format(Resources.ErrorWritingDebugSettings, fileName, ex.Message);
                LogError(err, false);
                throw;
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
        protected Dictionary<string, object> GetSettingsToSerialize(ILaunchSettings curSettings)
        {
            var profileData = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
            foreach (var profile in curSettings.Profiles)
            {
                if (ProfileShouldBePersisted(profile))
                {
                    profileData.Add(profile.Name, LaunchProfileData.ToSerializableForm(profile));
                }
            }

            Dictionary<string, object> dataToSave = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var setting in curSettings.GlobalSettings)
            {
                if(!setting.Value.IsInMemoryObject())
                {
                    dataToSave.Add(setting.Key, setting.Value);
                }
            }

            if (profileData.Count > 0)
            {
                dataToSave.Add(ProfilesSectionName, profileData);
            }

            return dataToSave;
        }

        /// <summary>
        /// Helper returns true if this is a profile which should be persisted. Filters out noaction profiles
        /// </summary>
        private bool ProfileShouldBePersisted(ILaunchProfile profile)
        {
            return !profile.IsInMemoryObject();
        }

        /// <summary>
        /// Helper to check out the debugsettings.json file
        /// </summary>
        protected async Task CheckoutSettingsFileAsync()
        {
            var sourceControlIntegration = SourceControlIntegrations.FirstOrDefault();
            if (sourceControlIntegration != null && sourceControlIntegration.Value != null)
            {
                string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

                await sourceControlIntegration.Value.CanChangeProjectFilesAsync(new[] { fileName }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handler for when the Launch settings file changes. Actually, we watch the project root so any
        /// file with the name LaunchSettings.json. We don't need to special case because, if a file with this name
        /// changes we will only check if the one we cared about was modified.
        /// </summary>
        protected void LaunchSettingsFile_Changed(object sender, FileSystemEventArgs e)
        {
            if (!IgnoreFileChanges)
            {
#pragma warning disable CS0618  // We're in a synchronous callback
                string fileName = LaunchSettingsFile;
#pragma warning restore CS0618

                // Only do something if the file is truly different than what we synced. Here, we want to 
                // throttle. 
                if (!FileManager.FileExists(fileName) || FileManager.LastFileWriteTime(fileName) != LastSettingsFileSyncTime)
                {
                    FileChangeScheduler.ScheduleAsyncTask(async token => {

                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        // Updates need to be sequenced
                        await _sequentialTaskQueue.ExecuteTask(async () =>
                                        {
                                            await UpdateProfilesAsync(null).ConfigureAwait(false);
                                        }).ConfigureAwait(false);
                    });
                }
            }
        }

        /// <summary>
        /// Makes sure the settings folder exists on disk. Doesn't add the folder to
        /// the project.
        /// </summary>
        protected async Task EnsureSettingsFolderAsync()
        {
            string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

            string parentPath = Path.GetDirectoryName(fileName);

            FileManager.CreateDirectory(parentPath);
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
        /// project oltherwise we force the project to have a properties folder.
        /// </summary>
        private void WatchLaunchSettingsFile()
        {
            if (FileWatcher == null)
            {
                // Create our scheduler for processing file chagnes
                FileChangeScheduler = new TaskDelayScheduler(FileChangeProcessingDelay, CommonProjectServices.ThreadingService,
                    ProjectServices.ProjectAsynchronousTasks.UnloadCancellationToken);

                FileWatcher = new SimpleFileWatcher(Path.GetDirectoryName(CommonProjectServices.Project.FullPath),
                                                    true,
                                                    NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
                                                    LaunchSettingsFilename,
                                                    LaunchSettingsFile_Changed,
                                                    LaunchSettingsFile_Changed);
            }
        }

        /// <summary>
        /// Need to amke sure we cleanup the dataflow links and file watcher
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

                if(_sequentialTaskQueue != null)
                {
                    _sequentialTaskQueue.Dispose();
                    _sequentialTaskQueue = null;
                }

                if (_broadcastBlock != null)
                {
                    _broadcastBlock.Complete();
                    _broadcastBlock = null;
                }

                if (ProjectRuleSubscriptionLink != null)
                {
                    ProjectRuleSubscriptionLink.Dispose();
                    ProjectRuleSubscriptionLink = null;
                }
            }
        }

        /// <summary>
        /// Replaces the current set of profiles with the contents of profiles. If changes were
        /// made, the file will be checked out and saved. Note it ignores the value of the active profile
        /// as this setting is controlled by a user property.
        /// </summary>
        public async Task UpdateAndSaveSettingsAsync(ILaunchSettings newSettings)
        {
            // Updates need to be sequenced. Do not call this version from within an ExecuteTask as it
            // will deadlock
            await _sequentialTaskQueue.ExecuteTask(async () =>
            {
                await UpdateAndSaveSettingsInternalAsync(newSettings).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Replaces the current set of profiles with the contents of profiles. If changes were
        /// made, the file will be checked out and saved. Note it ignores the value of the active profile
        /// as this setting is controlled by a user property.
        /// </summary>
        private async Task UpdateAndSaveSettingsInternalAsync(ILaunchSettings newSettings, bool persistToDisk = true)
        {
            await CheckoutSettingsFileAsync().ConfigureAwait(false);

            // Make sure the profiles are copied. We don't want them to mutate.
            var activeProfileName = ActiveProfile?.Name;

            ILaunchSettings newSnapshot = new LaunchSettings(newSettings.Profiles, newSettings.GlobalSettings, activeProfileName);
            if(persistToDisk)
            {
                await SaveSettingsToDiskAsync(newSettings).ConfigureAwait(false);
            }

            FinishUpdate(newSnapshot);
        }

        /// <summary>
        /// This function blocks until a snapshot is available. It will return null if the timeout occurs
        /// prior to the snapshot is available
        /// </summary>
        public async Task<ILaunchSettings> WaitForFirstSnapshot(int timeout)
        {
            if (CurrentSnapshot != null)
            {
                return CurrentSnapshot;
            }

            await _firstSnapshotCompletionSource.Task.TryWaitForCompleteOrTimeout(timeout).ConfigureAwait(false);
            return CurrentSnapshot;
        }

        /// <summary>
        /// Adds the given profile to the list and saves to disk. If a profile with the same 
        /// name exists (case sensitive), it will be replaced with the new profile. If addToFront is
        /// true the profile will be the first one in the list. This is useful since quite often callers want
        /// their just added profile to be listed first in the start menu. If addToFront is false but there is
        /// an existing profile, the new one will be inserted at the same location rather than at the end.
        /// </summary>
        public async Task AddOrUpdateProfileAsync(ILaunchProfile profile, bool addToFront)
        {
            // Updates need to be sequenced
            await _sequentialTaskQueue.ExecuteTask(async () =>
            {
                var currentSettings = await GetSnapshotThrowIfErrors().ConfigureAwait(false);
                ILaunchProfile existingProfile = null;
                int insertionIndex = 0;
                foreach (var p in currentSettings.Profiles)
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

                // If the new profile is in-nmemory only, we don't want to touch the disk unless it replaces an existing disk based
                // profile
                bool saveToDisk = !profile.IsInMemoryObject() || (existingProfile != null && !existingProfile.IsInMemoryObject());
                
                var newSnapshot = new LaunchSettings(profiles, currentSettings?.GlobalSettings, currentSettings?.ActiveProfile?.Name);
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes the specified profile from the list and saves to disk.
        /// </summary>
        public async Task RemoveProfileAsync(string profileName)
        {
            // Updates need to be sequenced
            await _sequentialTaskQueue.ExecuteTask(async () =>
            {
                var currentSettings = await GetSnapshotThrowIfErrors().ConfigureAwait(false);
                var existingProfile = currentSettings.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, profileName));
                if (existingProfile != null)
                {
                    var profiles = currentSettings.Profiles.Remove(existingProfile);
                   
                    // If the new profile is in-nmemory only, we don't want to touch the disk
                    bool saveToDisk = !existingProfile.IsInMemoryObject();
                    var newSnapshot = new LaunchSettings(profiles, currentSettings.GlobalSettings, currentSettings.ActiveProfile?.Name);
                    await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds or updates the global settings represented by settingName. Saves the 
        /// updated settings to disk. Note that the settings object must be serializable.
        /// </summary>
        public async Task AddOrUpdateGlobalSettingAsync(string settingName, object settingContent)
        {
            // Updates need to be sequenced
            await _sequentialTaskQueue.ExecuteTask(async () =>
            {
                var currentSettings = await GetSnapshotThrowIfErrors().ConfigureAwait(false);
                ImmutableDictionary<string, object> globalSettings = ImmutableDictionary<string, object>.Empty;
                if (currentSettings.GlobalSettings.TryGetValue(settingName, out var currentValue))
                {
                    globalSettings = currentSettings.GlobalSettings.Remove(settingName);
                }
                else
                {
                    globalSettings = currentSettings.GlobalSettings;
                }

                bool saveToDisk = !settingContent.IsInMemoryObject() || (currentValue != null && !currentValue.IsInMemoryObject());

                var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings.Add(settingName, settingContent), currentSettings.ActiveProfile?.Name);
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes the specified global setting and saves the settings to disk
        /// </summary>
        public async Task RemoveGlobalSettingAsync(string settingName)
        {
            // Updates need to be sequenced
            await _sequentialTaskQueue.ExecuteTask(async () =>
            {
                var currentSettings = await GetSnapshotThrowIfErrors().ConfigureAwait(false);
                if (currentSettings.GlobalSettings.TryGetValue(settingName, out var currentValue))
                {
                    bool saveToDisk = !currentValue.IsInMemoryObject();
                    var globalSettings = currentSettings.GlobalSettings.Remove(settingName);
                    var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings, currentSettings.ActiveProfile?.Name);
                    await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Helper retrieves the current snapshot (waiting up to 5s) and if there were errors in the launchsettings.json file
        /// or there isn't a snapshot, it throws an error. There should always be a snapshot of some kind returned
        /// </summary>
        public async Task<ILaunchSettings> GetSnapshotThrowIfErrors()
        {
            var currentSettings = await WaitForFirstSnapshot(WaitForFirstSnapshotDelay).ConfigureAwait(false);
            if (currentSettings == null || (currentSettings.Profiles.Count == 1 && string.Equals(currentSettings.Profiles[0].CommandName, ErrorProfileCommandName, StringComparison.Ordinal)))
            {
                string fileName = await GetLaunchSettingsFilePathAsync().ConfigureAwait(false);

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
            var props = await CommonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync().ConfigureAwait(false);
            await props.ActiveDebugProfile.SetValueAsync(profileName).ConfigureAwait(true);
        }

        internal async Task<string> GetLaunchSettingsFilePathNoCacheAsync()
        {
            // NOTE: To reduce behavior changes, we currently cache the folder that we get from the AppDesignerSpecialFileProvider, 
            // even though it can change over the lifetime of the project. We should fix this and convert to using dataflow  
            // see: https://github.com/dotnet/project-system/issues/2316.

            string folder = await _appDesignerSpecialFileProvider.Value.GetFileAsync(SpecialFiles.AppDesigner, SpecialFileFlags.FullPath)
                                                                       .ConfigureAwait(false);

            if (folder == null)  // AppDesigner capability not present, or the project has set AppDesignerFolder to empty
                folder = Path.GetDirectoryName(CommonProjectServices.Project.FullPath);

            return Path.Combine(folder, LaunchSettingsFilename);
        }
    }
}
