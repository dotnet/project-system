// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Debug;

/// <summary>
/// Produces and updates the <see cref="ILaunchSettings"/> for a project based on its <c>launchSettings.json</c> file.
/// </summary>
/// <remarks>
/// <para>
/// Listens for changes to both the <c>launchSettings.json</c> file and the <c>ActiveDebugProfile</c> property in the <c>.user</c> file.
/// When changes occur, an updated <see cref="ILaunchSettings"/> snapshot is published via Dataflow.
/// </para>
/// <para>
/// If joining this provider to a dataflow pipeline, use <see cref="IVersionedLaunchSettingsProvider"/> over
/// <see cref="ILaunchSettingsProvider.SourceBlock"/> so that project versions are propagated to downstream consumers.
/// </para>
/// <para>
/// Some methods are <see langword="protected"/> for testing purposes.
/// </para>
/// </remarks>
[Export(typeof(ILaunchSettingsProvider))]
[Export(typeof(ILaunchSettingsProvider2))]
[Export(typeof(ILaunchSettingsProvider3))]
[Export(typeof(IVersionedLaunchSettingsProvider))]
[AppliesTo(ProjectCapability.LaunchProfiles)]
internal class LaunchSettingsProvider : ProjectValueDataSourceBase<ILaunchSettings>, ILaunchSettingsProvider3, IVersionedLaunchSettingsProvider, IFileWatcherServiceClient
{
    public const string LaunchSettingsFilename = "launchSettings.json";

    // Command that means run this project
    public const string RunProjectCommandName = "Project";

    //  Command that means run an executable
    public const string RunExecutableCommandName = "Executable";

    // These are used internally to loop in debuggers to handle F5 when there are errors in
    // the launch settings file or when there are no profiles specified (like class libraries)
    public const string ErrorProfileCommandName = "ErrorProfile";
    private const string ErrorProfileErrorMessageSettingsKey = "ErrorString";

    private readonly TaskCompletionSource _firstSnapshotCompletionSource = new();

    // MEF imports
    private readonly UnconfiguredProject _project;
    private readonly IUnconfiguredProjectServices _projectServices;
    private readonly IFileSystem _fileSystem;
    private readonly IUnconfiguredProjectCommonServices _commonProjectServices;
    private readonly IFileWatcherService _fileWatcherService;
    private readonly IManagedProjectDiagnosticOutputService? _diagnosticOutputService;
    private readonly IActiveConfiguredProjectSubscriptionService? _projectSubscriptionService;
    private readonly IActiveConfiguredValue<ProjectProperties?> _projectProperties;
    private readonly IProjectFaultHandlerService _projectFaultHandler;

    private readonly AsyncLazy<string> _launchSettingsFilePath;
    private readonly SequentialTaskExecutor _sequentialTaskQueue;
    private readonly Lazy<LaunchProfile?> _defaultLaunchProfile;
    private readonly AsyncLazy<IFileWatcher?> _launchSettingFileWatcher;

    private IReceivableSourceBlock<ILaunchSettings>? _changedSourceBlock;
    private IBroadcastBlock<ILaunchSettings>? _broadcastBlock;
    private IReceivableSourceBlock<IProjectVersionedValue<ILaunchSettings>>? _versionedChangedSourceBlock;
    private IBroadcastBlock<IProjectVersionedValue<ILaunchSettings>>? _versionedBroadcastBlock;
    private ILaunchSettings? _currentSnapshot;
    private IDisposable? _projectRuleSubscriptionLink;
    private long _nextVersion = 1;

    [ImportingConstructor]
    public LaunchSettingsProvider(
        UnconfiguredProject project,
        IUnconfiguredProjectServices projectServices,
        IFileSystem fileSystem,
        IUnconfiguredProjectCommonServices commonProjectServices,
        IActiveConfiguredProjectSubscriptionService? projectSubscriptionService,
        IActiveConfiguredValue<ProjectProperties?> projectProperties,
        IProjectFaultHandlerService projectFaultHandler,
        IFileWatcherService fileWatchService,
        IManagedProjectDiagnosticOutputService? diagnosticOutputService,
        JoinableTaskContext joinableTaskContext)
        : base(projectServices, synchronousDisposal: false, registerDataSource: false)
    {
        _project = project;
        _projectServices = projectServices;
        _fileSystem = fileSystem;
        _commonProjectServices = commonProjectServices;
        _fileWatcherService = fileWatchService;
        _diagnosticOutputService = diagnosticOutputService;
        _projectSubscriptionService = projectSubscriptionService;
        _projectProperties = projectProperties;
        _projectFaultHandler = projectFaultHandler;

        DefaultLaunchProfileProviders = new OrderPrecedenceImportCollection<IDefaultLaunchProfileProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, project);
        JsonSerializationProviders = new OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, project);
        SourceControlIntegrations = new OrderPrecedenceImportCollection<ISourceCodeControlIntegration>(projectCapabilityCheckProvider: project);

        _sequentialTaskQueue = new SequentialTaskExecutor(new JoinableTaskContextNode(joinableTaskContext), nameof(LaunchSettingsProvider));

        _launchSettingsFilePath = new AsyncLazy<string>(GetLaunchSettingsFilePathNoCacheAsync, commonProjectServices.ThreadingService.JoinableTaskFactory);
        _launchSettingFileWatcher = new AsyncLazy<IFileWatcher?>(WatchLaunchSettingsFileAsync, commonProjectServices.ThreadingService.JoinableTaskFactory)
        {
            SuppressRecursiveFactoryDetection = true
        };

        _defaultLaunchProfile = new Lazy<LaunchProfile?>(() =>
        {
            ILaunchProfile? profile = DefaultLaunchProfileProviders?.FirstOrDefault()?.Value?.CreateDefaultProfile();

            return profile is null
                ? null
                : LaunchProfile.Clone(profile);
        });
    }

    [ImportMany]
    protected OrderPrecedenceImportCollection<ILaunchSettingsSerializationProvider, IJsonSection> JsonSerializationProviders { get; set; }

    [ImportMany]
    protected OrderPrecedenceImportCollection<ISourceCodeControlIntegration> SourceControlIntegrations { get; set; }

    [ImportMany]
    protected OrderPrecedenceImportCollection<IDefaultLaunchProfileProvider> DefaultLaunchProfileProviders { get; set; }

    // When we are saving the file we set this to minimize noise from the file change
    protected bool IgnoreFileChanges { get; set; }

    protected TimeSpan FileChangeProcessingDelay = TimeSpan.FromMilliseconds(500);

    public ITaskDelayScheduler? FileChangeScheduler { get; protected set; }

    // Tracks when we last read or wrote to the file. Prevents picking up needless changes
    protected DateTime LastSettingsFileSyncTimeUtc { get; set; }

    [Obsolete($"Use {nameof(GetLaunchSettingsFilePathAsync)} instead.")]
    public string LaunchSettingsFile => _commonProjectServices.ThreadingService.ExecuteSynchronously(GetLaunchSettingsFilePathAsync);

    public ILaunchProfile? ActiveProfile => CurrentSnapshot?.ActiveProfile;

    IReceivableSourceBlock<ILaunchSettings> ILaunchSettingsProvider.SourceBlock
    {
        get
        {
            EnsureInitialized();
            return _changedSourceBlock!;
        }
    }

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

    public override NamedIdentity DataSourceKey { get; } = new NamedIdentity(nameof(LaunchSettingsProvider));

    public override IComparable DataSourceVersion
    {
        // _nextVersion represents the version we will use in the future, so we need to
        // subtract 1 to get the current version.
        get => _nextVersion - 1;
    }

    public override IReceivableSourceBlock<IProjectVersionedValue<ILaunchSettings>> SourceBlock
    {
        get
        {
            EnsureInitialized();
            return _versionedChangedSourceBlock!;
        }
    }

    protected override void Initialize()
    {
        base.Initialize();

        // Create our broadcast block for subscribers to ILaunchSettingsProvider to get new ILaunchSettings information
        _broadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<ILaunchSettings>(nameFormat: "Launch Settings Broadcast: {1}");
        _changedSourceBlock = _broadcastBlock.SafePublicize();

        // Create our broadcast block for subscribers to IVersionedLaunchSettingsProvider to get new ILaunchSettings information
        _versionedBroadcastBlock = DataflowBlockSlim.CreateBroadcastBlock<IProjectVersionedValue<ILaunchSettings>>(nameFormat: "Versioned Launch Settings Broadcast: {1}");
        _versionedChangedSourceBlock = _versionedBroadcastBlock.SafePublicize();

        // Subscribe to changes to the broadcast block using the idle scheduler. This should filter out a lot of the intermediate
        // states that files can be in.
        if (_projectSubscriptionService is not null)
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

            JoinUpstreamDataSources(_projectSubscriptionService.ProjectRuleSource, _commonProjectServices.Project.Capabilities);
        }

        FileChangeScheduler?.Dispose();

        Assumes.Present(_projectServices.ProjectAsynchronousTasks);

        // Create our scheduler for processing file changes
        FileChangeScheduler = new TaskDelayScheduler(
            FileChangeProcessingDelay,
            _commonProjectServices.ThreadingService,
            _projectServices.ProjectAsynchronousTasks.UnloadCancellationToken);

        // establish the file watcher. We don't need wait this, because files can be changed in the system anyway, so blocking our process
        // doesn't provide real benefit. It is of course possible that the file is changed before the watcher is established. To eliminate this
        // gap, we can recheck file after the watcher is established. I will skip this for now.
        _project.Services.FaultHandler.Forget(
            _launchSettingFileWatcher.GetValueAsync(),
            _project,
            severity: ProjectFaultSeverity.LimitedFunctionality);
    }

    /// <summary>
    /// Handles changes to the ProjectDebugger properties. Gets the active profile and generates a launch settings update if it
    /// has changed. The first evaluation generally kicks off the first snapshot
    /// </summary>
    protected async Task ProjectRuleBlock_ChangedAsync(IProjectVersionedValue<ValueTuple<IProjectSubscriptionUpdate, IProjectCapabilitiesSnapshot>> projectSnapshot)
    {
        // Need to use JTF.RunAsync here to ensure that this task can coordinate with others.
        await JoinableFactory.RunAsync(async () =>
        {
            if (projectSnapshot.Value.Item1.CurrentState.TryGetValue(ProjectDebugger.SchemaName, out IProjectRuleSnapshot? ruleSnapshot))
            {
                ruleSnapshot.Properties.TryGetValue(ProjectDebugger.ActiveDebugProfileProperty, out string? activeProfile);
                ILaunchSettings snapshot = CurrentSnapshot;
                if (snapshot is null || !LaunchProfile.IsSameProfileName(activeProfile, snapshot.ActiveProfile?.Name))
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
        });
    }

    /// <summary>
    /// Called when the active profile has changed. If there is a current snapshot it just updates that. Otherwise, it creates
    /// a new snapshot
    /// </summary>
    protected async Task UpdateActiveProfileInSnapshotAsync(string? updatedActiveProfileName)
    {
        ILaunchSettings snapshot = CurrentSnapshot;
        if (snapshot is null || await SettingsFileHasChangedAsync())
        {
            await UpdateProfilesAsync(updatedActiveProfileName);
            return;
        }

        var newSnapshot = new LaunchSettings(snapshot.Profiles, snapshot.GlobalSettings, updatedActiveProfileName, version: GetNextVersion());
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
            if (updatedActiveProfileName is null)
            {
                ProjectDebugger props = await _commonProjectServices.ActiveConfiguredProjectProperties.GetProjectDebuggerPropertiesAsync();
                if (await props.ActiveDebugProfile.GetValueAsync() is IEnumValue activeProfileVal)
                {
                    updatedActiveProfileName = activeProfileVal.Name;
                }
            }

            var (profiles, globalSettings) = await ReadSettingsFileFromDiskAsync();

            // If there are no profiles, we will add a default profile to run the project. W/o it our debugger
            // won't be called on F5 and the user will see a poor error message
            if (profiles.Length == 0)
            {
                LaunchProfile? defaultLaunchProfile = _defaultLaunchProfile.Value;
                if (defaultLaunchProfile is not null)
                {
                    profiles = profiles.Add(defaultLaunchProfile);
                }
            }

            // If we have a previous snapshot, merge in in-memory profiles
            ILaunchSettings prevSnapshot = CurrentSnapshot;
            if (prevSnapshot is not null)
            {
                MergeExistingInMemoryProfiles(ref profiles, prevSnapshot.Profiles);
                MergeExistingInMemoryGlobalSettings(ref globalSettings, prevSnapshot.GlobalSettings);
            }

            var newSnapshot = new LaunchSettings(
                profiles,
                globalSettings.ToImmutableDictionary(pair => pair.Name, pair => pair.Value, StringComparers.LaunchSettingsPropertyNames),
                updatedActiveProfileName,
                version: GetNextVersion());

            FinishUpdate(newSnapshot);
        }
        catch (Exception ex)
        {
            // Errors are added as error list entries. We don't want to throw out of here
            // However, if we have never created a snapshot it means there is some error in the file and we want
            // to have the user see that, so we add a dummy profile which will bind to an existing debugger which will
            // display the error when run
            if (CurrentSnapshot is null)
            {
                var errorProfile = new LaunchProfile(
                    name: Resources.NoActionProfileName,
                    commandName: ErrorProfileCommandName,
                    doNotPersist: true,
                    otherSettings: ImmutableArray.Create((ErrorProfileErrorMessageSettingsKey, (object)ex.Message)));
                var snapshot = new LaunchSettings(new[] { errorProfile }, null, errorProfile.Name, version: GetNextVersion());
                FinishUpdate(snapshot);
            }
        }

        // Re-applies in-memory profiles to the newly created snapshot. Note that we don't want to merge in the error
        // profile
        static void MergeExistingInMemoryProfiles(ref ImmutableArray<LaunchProfile> newProfiles, ImmutableList<ILaunchProfile> previousProfiles)
        {
            for (int i = 0; i < previousProfiles.Count; i++)
            {
                ILaunchProfile profile = previousProfiles[i];

                if (profile.IsInMemoryObject() && !string.Equals(profile.CommandName, ErrorProfileCommandName, StringComparisons.LaunchProfileCommandNames))
                {
                    // Does it already have one with this name?
                    if (newProfiles.FirstOrDefault((p1, p2) => LaunchProfile.IsSameProfileName(p1.Name, p2.Name), profile) is null)
                    {
                        // Create a new one from the existing in-memory profile and insert it in the same location, or the end if it
                        // is beyond the end of the list
                        if (i > newProfiles.Length)
                        {
                            newProfiles = newProfiles.Add(LaunchProfile.Clone(profile));
                        }
                        else
                        {
                            newProfiles = newProfiles.Insert(i, LaunchProfile.Clone(profile));
                        }
                    }
                }
            }
        }

        // Re-applies in-memory global options to the newly created snapshot
        static void MergeExistingInMemoryGlobalSettings(ref ImmutableArray<(string Name, object Value)> newGlobalSettings, ImmutableDictionary<string, object>? previousGlobalSettings)
        {
            if (previousGlobalSettings is not null)
            {
                foreach ((string key, object value) in previousGlobalSettings)
                {
                    if (value.IsInMemoryObject() && !newGlobalSettings.Any(pair => StringComparers.LaunchSettingsPropertyNames.Equals(pair.Name, key)))
                    {
                        newGlobalSettings = newGlobalSettings.Add((key, value));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the file has changed since we last read it,
    /// or if the file does not exist.
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
            var versionedLaunchSettings = (IVersionedLaunchSettings)newSnapshot;
            _versionedBroadcastBlock?.Post(new ProjectVersionedValue<ILaunchSettings>(
                newSnapshot,
                Empty.ProjectValueVersions.Add(DataSourceKey, versionedLaunchSettings.Version)));
            _broadcastBlock?.Post(newSnapshot);
        }
    }

    /// <summary>
    /// Reads <c>launchSettings.json</c> and returns all data as an API object.
    /// </summary>
    /// <remarks>
    /// Returns empty collections if the file does not exist.
    /// </remarks>
    /// <exception cref="Newtonsoft.Json.JsonReaderException">JSON data was not of the expected format.</exception>
    protected async Task<(ImmutableArray<LaunchProfile> Profiles, ImmutableArray<(string Name, object Value)> GlobalSettings)> ReadSettingsFileFromDiskAsync()
    {
        string fileName = await GetLaunchSettingsFilePathAsync();

        if (!_fileSystem.FileExists(fileName))
        {
            return (ImmutableArray<LaunchProfile>.Empty, ImmutableArray<(string Name, object Value)>.Empty);
        }

        using Stream stream = _fileSystem.OpenTextStream(fileName);

        // read launch settings file into a memory stream with async method,
        // because the Json reader doesn't support async reading directly,
        // and traces show that it blocks multiple threads at the same time during solution load.
        // Most settings files are small, so we load them into memory.
        var bufferStream = new MemoryStream((int)stream.Length);
        await stream.CopyToAsync(bufferStream);

        bufferStream.Position = 0; // Reset the stream position to the beginning for reading
        var result = LaunchSettingsJsonEncoding.FromJson(new StreamReader(bufferStream), JsonSerializationProviders);

        // Remember the time we are sync'd to.
        // Only do this when we successfully obtain a result.
        LastSettingsFileSyncTimeUtc = _fileSystem.GetLastFileWriteTimeOrMinValueUtc(fileName);

        return result;
    }

    public Task<string> GetLaunchSettingsFilePathAsync()
    {
        return _launchSettingsFilePath.GetValueAsync();
    }

    /// <summary>
    /// Saves the launch settings to the launch settings file. Adds an error string and throws if an exception. Note
    /// that the caller is responsible for checking out the file
    /// </summary>
    protected async Task SaveSettingsToDiskAsync(ILaunchSettings newSettings)
    {
        string fileName = await GetLaunchSettingsFilePathAsync();

        try
        {
            await EnsureSettingsFolderAsync();

            string json = LaunchSettingsJsonEncoding.ToJson(
                profiles: newSettings.Profiles,
                globalSettings: newSettings.GlobalSettings.Select(pair => (pair.Key, pair.Value)));

            // Ignore notifications of edits while our edit is in flight.
            IgnoreFileChanges = true;

            await _fileSystem.WriteAllTextAsync(fileName, json);

            // Update the last write time
            LastSettingsFileSyncTimeUtc = _fileSystem.GetLastFileWriteTimeOrMinValueUtc(fileName);
        }
        finally
        {
            IgnoreFileChanges = false;
        }
    }

    /// <summary>
    /// Helper to check out the <c>launchSettings.json</c> file, if needed.
    /// </summary>
    protected async Task CheckoutSettingsFileAsync()
    {
        Lazy<ISourceCodeControlIntegration>? sourceControlIntegration = SourceControlIntegrations.FirstOrDefault();

        if (sourceControlIntegration?.Value is not null)
        {
            string fileName = await GetLaunchSettingsFilePathAsync();

            await sourceControlIntegration.Value.CanChangeProjectFilesAsync([fileName]);
        }
    }

    protected async Task HandleLaunchSettingsFileChangedAsync()
    {
        if (IgnoreFileChanges)
        {
            return;
        }

        string fileName = await GetLaunchSettingsFilePathAsync();

        // Only do something if the file is truly different than what we synced. Here, we want to
        // throttle.
        if (_fileSystem.GetLastFileWriteTimeOrMinValueUtc(fileName) != LastSettingsFileSyncTimeUtc)
        {
            Assumes.NotNull(FileChangeScheduler);

            try
            {
                await FileChangeScheduler.ScheduleAsyncTask(token =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return Task.CompletedTask;
                    }

                    // Updates need to be sequenced
                    return _sequentialTaskQueue.ExecuteTask(() => UpdateProfilesAsync(null));
                });
            }
            catch (ObjectDisposedException)
            {
                // during closing the FileChangeScheduler can be disposed while the task to process the last file change is still running.
            }
        }
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
    /// Sets up a file system watcher to look for changes to the launchsettings.json file. It watches at the root of the
    /// project otherwise we force the project to have a properties folder.
    /// </summary>
    private async Task<IFileWatcher?> WatchLaunchSettingsFileAsync()
    {
        Assumes.Present(_projectServices.ProjectAsynchronousTasks);
        CancellationToken cancellationToken = _projectServices.ProjectAsynchronousTasks.UnloadCancellationToken;

        IFileWatcher? fileWatcher = null;

        await TaskScheduler.Default;

        string launchSettingsToWatch = await GetLaunchSettingsFilePathAsync();
        if (launchSettingsToWatch is not null)
        {
            fileWatcher = await _fileWatcherService.CreateFileWatcherAsync(this, FileWatchChangeKinds.Changed | FileWatchChangeKinds.Added | FileWatchChangeKinds.Removed, cancellationToken);

            try
            {
                // disposing the file watcher will unregister the file, so we don't have to track the cookie.
                _ = await fileWatcher.RegisterFileAsync(launchSettingsToWatch, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException)
            {
                // If the project folder is no longer available this will throw, which can happen during branch switching
                await fileWatcher.DisposeAsync();
                fileWatcher = null;
            }
        }

        return fileWatcher;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _project.Services.FaultHandler.Forget(
                _launchSettingFileWatcher.DisposeValueAsync(),
                _project,
                severity: ProjectFaultSeverity.Recoverable);

            FileChangeScheduler?.Dispose();
            FileChangeScheduler = null;

            _sequentialTaskQueue.Dispose();
            
            _versionedBroadcastBlock?.Complete();
            _versionedBroadcastBlock = null;
            
            _broadcastBlock?.Complete();
            _broadcastBlock = null;
            
            _projectRuleSubscriptionLink?.Dispose();
            _projectRuleSubscriptionLink = null;

            _firstSnapshotCompletionSource.TrySetCanceled();
        }
    }

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

        ILaunchSettings newSnapshot = new LaunchSettings(newSettings.Profiles, newSettings.GlobalSettings, activeProfileName, version: GetNextVersion());
        if (persistToDisk)
        {
            await SaveSettingsToDiskAsync(newSettings);
        }

        FinishUpdate(newSnapshot);
    }

    public async Task<ILaunchSettings?> WaitForFirstSnapshot(int timeout)
    {
        if (CurrentSnapshot is not null)
        {
            return CurrentSnapshot;
        }

        Assumes.Present(_projectServices.ProjectAsynchronousTasks);

        // Ensure we don't hang if the project is unloaded before we get a snapshot
        Task task = _firstSnapshotCompletionSource.Task.WithCancellation(_projectServices.ProjectAsynchronousTasks.UnloadCancellationToken);

        if (await task.TryWaitForCompleteOrTimeoutAsync(timeout))
        {
            Assumes.NotNull(CurrentSnapshot);
        }

        return CurrentSnapshot;
    }

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
            if (existingProfile is not null)
            {
                profiles = currentSettings.Profiles.Remove(existingProfile);
            }
            else
            {
                profiles = currentSettings.Profiles;
            }

            if (addToFront)
            {
                profiles = profiles.Insert(0, LaunchProfile.Clone(profile));
            }
            else
            {
                // Insertion index will be set to the current count (end of list) if an existing item was not found otherwise
                // it will point to where the previous one was found
                profiles = profiles.Insert(insertionIndex, LaunchProfile.Clone(profile));
            }

            // If the new profile is in-memory only, we don't want to touch the disk unless it replaces an existing disk based
            // profile
            bool saveToDisk = !profile.IsInMemoryObject() || (existingProfile?.IsInMemoryObject() == false);

            var newSnapshot = new LaunchSettings(profiles, currentSettings?.GlobalSettings, currentSettings?.ActiveProfile?.Name, version: GetNextVersion());
            await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
        });
    }

    public Task RemoveProfileAsync(string profileName)
    {
        // Updates need to be sequenced
        return _sequentialTaskQueue.ExecuteTask(async () =>
        {
            ILaunchSettings currentSettings = await GetSnapshotThrowIfErrorsAsync();
            ILaunchProfile? existingProfile = currentSettings.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, profileName));
            if (existingProfile is not null)
            {
                ImmutableList<ILaunchProfile> profiles = currentSettings.Profiles.Remove(existingProfile);

                // If the new profile is in-memory only, we don't want to touch the disk
                bool saveToDisk = !existingProfile.IsInMemoryObject();
                var newSnapshot = new LaunchSettings(profiles, currentSettings.GlobalSettings, currentSettings.ActiveProfile?.Name, version: GetNextVersion());
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

            var newSnapshot = new LaunchSettings(profiles, currentSettings?.GlobalSettings, currentSettings?.ActiveProfile?.Name, version: GetNextVersion());
            await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);

            return true;
        });
    }

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

            var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings.Add(settingName, settingContent), currentSettings.ActiveProfile?.Name, version: GetNextVersion());
            await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
        });
    }

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
                var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings, currentSettings.ActiveProfile?.Name, version: GetNextVersion());
                await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
            }
        });
    }

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

            var newSnapshot = new LaunchSettings(currentSettings.Profiles, globalSettings, currentSettings.ActiveProfile?.Name, version: GetNextVersion());
            await UpdateAndSaveSettingsInternalAsync(newSnapshot, saveToDisk);
        });
    }

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

        if (_project.Services.ActiveConfiguredProjectProvider is IActiveConfiguredProjectProvider activeConfiguredProjectProvider &&
            activeConfiguredProjectProvider.ActiveConfiguredProject is null)
        {
            // in a project system the LauchProfile is turned on through project factory, the provider can be initialized before configuration is loaded.
            // Because _projectProperties is depending on the active configured project, it will end up with NFE failure.
            await activeConfiguredProjectProvider.ActiveConfiguredProjectBlock.ReceiveAsync(_project.Services.ProjectAsynchronousTasks?.UnloadCancellationToken ?? CancellationToken.None).ConfigureAwaitRunInline();
        }

        // Default to the project directory if we're not able to get the AppDesigner folder.
        string folder = _commonProjectServices.Project.GetProjectDirectory();

        if (_projectProperties.Value is not null)
        {
            AppDesigner appDesignerProperties = await _projectProperties.Value.GetAppDesignerPropertiesAsync().ConfigureAwaitRunInline();
            if (await appDesignerProperties.FolderName.GetValueAsync().ConfigureAwaitRunInline() is string appDesignerFolderName)
            {
                folder = Path.Combine(folder, appDesignerFolderName);
            }
        }

        return Path.Combine(folder, LaunchSettingsFilename);
    }

    /// <summary>
    /// Helper retrieves the current snapshot and if there were errors in the launchSettings.json file
    /// or there isn't a snapshot, it throws an error. There should always be a snapshot of some kind returned.
    /// </summary>
    private async Task<ILaunchSettings> GetSnapshotThrowIfErrorsAsync()
    {
        ILaunchSettings? currentSettings = await WaitForFirstSnapshot(Timeout.Infinite);
        Assumes.NotNull(currentSettings);

        if (currentSettings.Profiles.Count == 1 && string.Equals(currentSettings.Profiles[0].CommandName, ErrorProfileCommandName, StringComparisons.LaunchProfileCommandNames))
        {
            string fileName = await GetLaunchSettingsFilePathAsync();

            if (currentSettings.Profiles[0].OtherSettings is { } otherSettings
                && otherSettings.TryGetValue(ErrorProfileErrorMessageSettingsKey, out object? errorMessageObject))
            {
                throw new Exception(string.Format(Resources.JsonErrorsNeedToBeCorrected_WithErrorMessage_2, fileName, errorMessageObject));
            }
            else
            {
                throw new Exception(string.Format(Resources.JsonErrorsNeedToBeCorrected_1, fileName));
            }
        }

        return currentSettings;
    }

    private long GetNextVersion() => _nextVersion++;

    /// <summary>
    /// Protected method for testing purposes.
    /// </summary>
    protected void SetNextVersion(long nextVersion) => _nextVersion = nextVersion;

    public void OnFilesChanged(IReadOnlyCollection<(string FilePath, FileWatchChangeKinds FileWatchChangeKinds)> changes)
    {
        _diagnosticOutputService?.WriteLine(string.Join(Environment.NewLine, changes.Select(change => $"File changed: {change.FilePath} ({change.FileWatchChangeKinds})")));
        _projectFaultHandler.Forget(HandleLaunchSettingsFileChangedAsync(), _project);
    }
}
