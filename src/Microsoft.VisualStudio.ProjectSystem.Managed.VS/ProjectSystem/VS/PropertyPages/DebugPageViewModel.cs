// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class DebugPageViewModel : PropertyPageViewModel, INotifyDataErrorInfo
    {
        private readonly string _executableFilter = string.Format("{0} (*.exe)|*.exe|{1} (*.*)|*.*", PropertyPageResources.ExecutableFiles, PropertyPageResources.AllFiles);
        private IDisposable _debugProfileProviderLink;

        // Unit Tests only
        private TaskCompletionSource<bool> _firstSnapshotCompleteSource = null;
        
        private IProjectThreadingService _projectThreadingService;
        private IProjectThreadingService ProjectThreadingService
        {
            get
            {
                if (_projectThreadingService == null)
                {
                    IUnconfiguredProjectVsServices _projectVsServices = UnconfiguredProject.Services.ExportProvider.GetExportedValue<IUnconfiguredProjectVsServices>();
                    _projectThreadingService = _projectVsServices.ThreadingService;

                }
                return _projectThreadingService;
            }
        }

        // The collection of UI providers.
        private OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> _uiProviders;

        // Tracks the current set of settings being worked on
        private IWritableLaunchSettings CurrentLaunchSettings { get; set; }

        public event EventHandler ClearEnvironmentVariablesGridError;
        public event EventHandler FocusEnvironmentVariablesGridRow;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public DebugPageViewModel()
        {
            // Hook into our own property changed event. This is solely to know when an active profile has been edited
            PropertyChanged += ViewModel_PropertyChanged;
        }


        // for unit testing
        internal DebugPageViewModel(TaskCompletionSource<bool> snapshotComplete, UnconfiguredProject unconfiguredProject)
        {
            _firstSnapshotCompleteSource = snapshotComplete;
            UnconfiguredProject = unconfiguredProject;
            PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// This is here so that we can clear the in-memory status of the active profile if it has been edited. This is
        /// so that the profile, and hence the users customizations, will be saved to disk
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IgnoreEvents)
            {
                if(SelectedDebugProfile != null && SelectedDebugProfile is IWritablePersistOption writablePersist)
                {
                    writablePersist.DoNotPersist = false;
                }
            }
        }
        
        private List<LaunchType> _launchTypes;
        public List<LaunchType> LaunchTypes
        {
            get
            {
                return _launchTypes;
            }
            set
            {
                OnPropertyChanged(ref _launchTypes, value);
            }
        }
        
        private LaunchType _selectedLaunchType;
        public LaunchType SelectedLaunchType
        {
            get
            {
                return _selectedLaunchType;
            }
            set
            {
                var oldActiveProvider = ActiveProvider;
                if (OnPropertyChanged(ref _selectedLaunchType, value))
                {
                    // Existing commands are shown in the UI as project
                    // since that is what is run when that command is selected. However, we don't want to just update the actual
                    // profile to this value - we want to treat them as equivalent.
                    if (_selectedLaunchType != null)
                    {
                        SelectedDebugProfile.CommandName = _selectedLaunchType.CommandName;
                        if (_selectedLaunchType.CommandName == ProfileCommandNames.Executable)
                        {
                            if (ExecutablePath == null)
                            {
                                ExecutablePath = string.Empty;
                            }
                        }
                        else
                        {
                            ExecutablePath = null;
                        }

                        // Let the active provider know of the changes. 
                        SwitchProviders(oldActiveProvider);

                        // These are all controlled by the ui provider and is affected by changing the launch type
                        OnPropertyChanged(nameof(SupportsExecutable));
                        OnPropertyChanged(nameof(SupportsArguments));
                        OnPropertyChanged(nameof(SupportsWorkingDirectory));
                        OnPropertyChanged(nameof(SupportsLaunchUrl));
                        OnPropertyChanged(nameof(SupportsEnvironmentVariables));
                        OnPropertyChanged(nameof(ActiveProviderUserControl));
                        OnPropertyChanged(nameof(DoesNotHaveErrors));
                    }
                }
            }
        }
        
        /// <summary>
        /// If we have an existing CustomControl, we disconnect from its change notifications
        /// and hook into the new ones. Assumes the activeProvider has already been changed
        /// </summary>
        public void SwitchProviders(ILaunchSettingsUIProvider oldProvider)
        {
            // Get the old custom control and disconnect from notifications
            if (oldProvider?.CustomUI?.DataContext is INotifyPropertyChanged context)
            {
                context.PropertyChanged -= OnCustomUIStateChanged;
                if (context is INotifyDataErrorInfo)
                {
                    ((INotifyDataErrorInfo)context).ErrorsChanged -= OnCustomUIErrorsChanged;
                }
            }

            // Now hook into the current providers notifications. We do that after having set the profile on the provider
            // so that we don't get notifications while the control is initializing. Note that this is likely the first time the 
            // custom control is asked for and we want to call it and have it created prior to setting the active profile
            var customControl = ActiveProvider?.CustomUI;
            if(customControl != null)
            {
                ActiveProvider.ProfileSelected(CurrentLaunchSettings);

                context = customControl.DataContext as INotifyPropertyChanged;
                if(context != null)
                {
                    context.PropertyChanged += OnCustomUIStateChanged;
                }

                if(context is INotifyDataErrorInfo notifyDataErrorInfo)
                {
                    notifyDataErrorInfo.ErrorsChanged += OnCustomUIErrorsChanged;
                }
            }
        }

        /// <summary>
        /// Called from the CustomUI when a change occurs. This just fires a dummy property change
        /// to dirty the page.
        /// </summary>
        private void OnCustomUIStateChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("CustomUIDirty");
        }

        public string CommandLineArguments
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return string.Empty;
                }

                return SelectedDebugProfile.CommandLineArgs;
            }
            set
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile.CommandLineArgs != value)
                {
                    SelectedDebugProfile.CommandLineArgs = value;
                    OnPropertyChanged(nameof(CommandLineArguments));
                }
            }
        }

        public string ExecutablePath
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return string.Empty;
                }

                return SelectedDebugProfile.ExecutablePath;
            }
            set
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile.ExecutablePath != value)
                {
                    SelectedDebugProfile.ExecutablePath = value;
                    OnPropertyChanged(nameof(ExecutablePath));
                }
            }
        }

        public string LaunchPage
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return string.Empty;
                }

                return SelectedDebugProfile.LaunchUrl;
            }
            set
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile.LaunchUrl != value)
                {
                    SelectedDebugProfile.LaunchUrl = value;
                    OnPropertyChanged(nameof(LaunchPage));
                }
            }
        }

        public string WorkingDirectory
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return string.Empty;
                }

                return SelectedDebugProfile.WorkingDirectory;
            }
            set
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile.WorkingDirectory != value)
                {
                    SelectedDebugProfile.WorkingDirectory = value;
                    OnPropertyChanged(nameof(WorkingDirectory));
                }
            }
        }

        public bool HasLaunchOption
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return false;
                }

                return SelectedDebugProfile.LaunchBrowser;
            }
            set
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile.LaunchBrowser != value)
                {
                    SelectedDebugProfile.LaunchBrowser = value;
                    OnPropertyChanged(nameof(HasLaunchOption));
                }
            }
        }

        public bool SupportsExecutable
        {
            get 
            {  
                return ActiveProviderSupportsProperty(UIProfilePropertyName.Executable); 
            }
        }

        public bool SupportsArguments
        {
            get 
            {  
                return ActiveProviderSupportsProperty(UIProfilePropertyName.Arguments); 
            }
        }

        public bool SupportsWorkingDirectory
        {
            get 
            {  
                return ActiveProviderSupportsProperty(UIProfilePropertyName.WorkingDirectory); 
            }
        }

        public bool SupportsLaunchUrl
        {
            get 
            {  
                return ActiveProviderSupportsProperty(UIProfilePropertyName.LaunchUrl); 
            }
        }

        public bool SupportsEnvironmentVariables
        {
            get 
            {  
                return ActiveProviderSupportsProperty(UIProfilePropertyName.EnvironmentVariables); 
            }
        }

        /// <summary>
        /// Helper returns true if there is an active provider and it supports the specified property
        /// </summary>
        private bool ActiveProviderSupportsProperty(string propertyName)
        {
            var activeProvider = ActiveProvider;
            return activeProvider?.ShouldEnableProperty(propertyName) ?? false;
        }

        public bool IsProfileSelected
        {
            get
            {
                return SelectedDebugProfile != null;
            }
        }

        private  ObservableCollection<IWritableLaunchProfile> _launchProfiles = new ObservableCollection<IWritableLaunchProfile>();
        public ObservableCollection<IWritableLaunchProfile> LaunchProfiles
        {
            get
            {
                return _launchProfiles;
            }
        }

        /// <summary>
        /// Helper called when a profile is added (new profile command), or a profile is deleted (delete profile command)
        /// </summary>
        private void NotifyProfileCollectionChanged()
        {
            OnPropertyChanged(nameof(HasProfiles));
            OnPropertyChanged(nameof(NewProfileEnabled));
        }
        
        public bool HasProfiles
        {
            get
            {
                return CurrentLaunchSettings != null && CurrentLaunchSettings.Profiles.Count > 0;
            }
        }

        /// <summary>
        /// Use the active profile in the CurrentLaunchSettings
        /// </summary>
        public IWritableLaunchProfile SelectedDebugProfile
        {
            get
            {
                return CurrentLaunchSettings?.ActiveProfile;
            }
            set
            {
                if (CurrentLaunchSettings != null && CurrentLaunchSettings.ActiveProfile != value)
                {
                    var oldProfile = CurrentLaunchSettings.ActiveProfile;
                    CurrentLaunchSettings.ActiveProfile = value;
                    NotifySelectedChanged(oldProfile);
                }
            }
        }
        
        private bool _removeEnvironmentVariablesRow;
        public bool RemoveEnvironmentVariablesRow
        {
            get
            {
                return _removeEnvironmentVariablesRow;
            }
            set
            {
                OnPropertyChanged(ref _removeEnvironmentVariablesRow, value, suppressInvalidation: true);
            }
        }

        private int _environmentVariablesRowSelectedIndex = -1;
        public int EnvironmentVariablesRowSelectedIndex
        {
            get { return _environmentVariablesRowSelectedIndex; }
            set
            {
                if (_environmentVariablesRowSelectedIndex != value)
                {
                    _environmentVariablesRowSelectedIndex = value;
                    if (_environmentVariablesRowSelectedIndex == -1)
                    {
                        //No selected item - Disable Remove button
                        RemoveEnvironmentVariablesRow = false;
                    }
                    else
                    {
                        RemoveEnvironmentVariablesRow = (EnvironmentVariablesValid) ? true : ((EnvironmentVariables[_environmentVariablesRowSelectedIndex] as NameValuePair).HasValidationError == true);
                    }

                    OnPropertyChanged(nameof(EnvironmentVariablesRowSelectedIndex), suppressInvalidation: true);
                }
            }
        }

        private bool _environmentVariablesValid = true;
        public bool EnvironmentVariablesValid
        {
            get
            {
                if (EnvironmentVariables == null) { return true; }
                else { return _environmentVariablesValid; }
            }
            set
            {
                if (_environmentVariablesValid != value)
                {
                    _environmentVariablesValid = value;
                    if (value == true && ClearEnvironmentVariablesGridError != null)
                    {
                        ClearEnvironmentVariablesGridError.Invoke(this, EventArgs.Empty);
                    }
                    OnPropertyChanged(nameof(EnvironmentVariablesValid), suppressInvalidation: true);
                }
            }
        }

        private ObservableList<NameValuePair> _environmentVariables;
        public ObservableList<NameValuePair> EnvironmentVariables
        {
            get
            {
                return _environmentVariables;
            }
            set
            {
                OnPropertyChanged(ref _environmentVariables, value);
            }
        }

        /// <summary>
        /// Provides binding to the current UI Provider usercontrol. 
        /// </summary>
        public UserControl ActiveProviderUserControl
        {
            get 
            {
                var provider = ActiveProvider;
                return ActiveProvider?.CustomUI;
            }
        }

        /// <summary>
        /// Called when the selection does change. Note that this code relies on the fact the current selection has been
        /// updated
        /// </summary>
        protected virtual void NotifySelectedChanged(IWritableLaunchProfile oldProfile)
        {
            // we need to keep the property page control from setting IsDirty when we are just switching between profiles.
            // we still need to notify the display of the changes though
            PushIgnoreEvents();
            try
            {

                // these have no backing store in the viewmodel, we need to send notifications when we change selected profiles
                // consider a better way of doing this
                OnPropertyChanged(nameof(SelectedDebugProfile));
                OnPropertyChanged(nameof(CommandLineArguments));
                OnPropertyChanged(nameof(ExecutablePath));
                OnPropertyChanged(nameof(LaunchPage));
                OnPropertyChanged(nameof(HasLaunchOption));
                OnPropertyChanged(nameof(WorkingDirectory));
                
                UpdateLaunchTypes();

                OnPropertyChanged(nameof(IsProfileSelected));
                OnPropertyChanged(nameof(DeleteProfileEnabled));
                
                SetEnvironmentGrid(oldProfile);

                UpdateActiveProfile();
            }
            finally
            {
                PopIgnoreEvents();
            }
        }

        private void UpdateActiveProfile()
        {
            // need to set it dirty so Apply() actually saves the profile
            // Billhie: this causes hangs. Disabling for now
            //if (this.ParentControl != null)
            //{
            //    this.ParentControl.IsDirty = true;
            //    WaitForAsync<int>(this.ParentControl.Apply);
            //}
        }

        /// <summary>
        /// Functions which actually does the save of the settings. Persists the changes to the launch settings
        /// file and configures IIS if needed.
        /// </summary>
        public async virtual System.Threading.Tasks.Task SaveLaunchSettings()
        {
            ILaunchSettingsProvider provider = GetDebugProfileProvider();
            if (EnvironmentVariables != null && EnvironmentVariables.Count > 0 && SelectedDebugProfile != null)
            {
                SelectedDebugProfile.EnvironmentVariables.Clear();
                foreach(var kvp in EnvironmentVariables)
                {
                    SelectedDebugProfile.EnvironmentVariables.Add(kvp.Name, kvp.Value);
                }
            }
            else if (SelectedDebugProfile != null)
            {
                SelectedDebugProfile.EnvironmentVariables.Clear();
            }

            await provider.UpdateAndSaveSettingsAsync(CurrentLaunchSettings.ToLaunchSettings()).ConfigureAwait(false);
        }

        private void SetEnvironmentGrid(IWritableLaunchProfile oldProfile)
        {
            if (EnvironmentVariables != null && oldProfile != null)
            {
                if (_environmentVariables.Count > 0)
                {
                    oldProfile.EnvironmentVariables.Clear();
                    foreach(var kvp in EnvironmentVariables)
                    {
                        oldProfile.EnvironmentVariables.Add(kvp.Name, kvp.Value);
                    }
                }
                else
                {
                    oldProfile.EnvironmentVariables.Clear();
                }
                EnvironmentVariables.ValidationStatusChanged -= EnvironmentVariables_ValidationStatusChanged;
                EnvironmentVariables.CollectionChanged -= EnvironmentVariables_CollectionChanged;
                ((INotifyPropertyChanged)EnvironmentVariables).PropertyChanged -= DebugPageViewModel_EnvironmentVariables_PropertyChanged;
            }

            if (SelectedDebugProfile != null)
            {
                EnvironmentVariables = SelectedDebugProfile.EnvironmentVariables.CreateList();
            }
            else
            {
                EnvironmentVariables = new ObservableList<NameValuePair>();
            }
            EnvironmentVariables.ValidationStatusChanged += EnvironmentVariables_ValidationStatusChanged;
            EnvironmentVariables.CollectionChanged += EnvironmentVariables_CollectionChanged;
            ((INotifyPropertyChanged)EnvironmentVariables).PropertyChanged += DebugPageViewModel_EnvironmentVariables_PropertyChanged;
        }

        private void EnvironmentVariables_ValidationStatusChanged(object sender, EventArgs e)
        {
            ValidationStatusChangedEventArgs args = e as ValidationStatusChangedEventArgs;
            EnvironmentVariablesValid = args.ValidationStatus;
        }

        /// <summary>
        /// Called whenever the debug targets change. Note that after a save this function will be
        /// called. It looks for changes and applies them to the UI as needed. Switching profiles
        /// will also cause this to change as the active profile is stored in the profiles snapshot.
        /// </summary>
        internal virtual void InitializeDebugTargetsCore(ILaunchSettings profiles)
        {
            var newSettings = profiles.ToWritableLaunchSettings();

            // Since this get's reentered if the user saves or the user switches active profiles.
            if (CurrentLaunchSettings != null && !CurrentLaunchSettings.SetttingsDiffer(newSettings))
            {
                return;
            }

            try
            {
                // This should never change the dirty state when loading the dialog
                PushIgnoreEvents();

                // Remember the current selection
                string curProfileName = SelectedDebugProfile?.Name;

                // Update the set of settings and generate a property change so the list of profiles gets updated. Note that we always
                // clear the active profile on the CurrentLaunchSettings so that when we do set one and property changed event is set
                CurrentLaunchSettings = newSettings;
                CurrentLaunchSettings.ActiveProfile = null;

                // Reload the launch profiles collection
                LaunchProfiles.Clear();
                foreach(var profile in CurrentLaunchSettings.Profiles)
                {
                    LaunchProfiles.Add(profile);
                }

                // When loading new profiles we need to clear the launch type. This is so the external changes cause the current 
                // active provider to be refreshed
                _selectedLaunchType = null;
                NotifyProfileCollectionChanged();

                // If we have a selection, we want to leave it as is
                if (curProfileName == null || newSettings.Profiles.FirstOrDefault(p => LaunchProfile.IsSameProfileName(p.Name, curProfileName)) == null)
                {
                    // Note that we have to be careful since the collection can be empty. 
                    if (profiles.ActiveProfile != null && !string.IsNullOrEmpty(profiles.ActiveProfile.Name))
                    {
                        SelectedDebugProfile = LaunchProfiles.Where(p => LaunchProfile.IsSameProfileName(p.Name, profiles.ActiveProfile.Name)).Single();
                    }
                    else
                    {
                        if (LaunchProfiles.Count > 0)
                        {
                            SelectedDebugProfile = LaunchProfiles[0];
                        }
                        else
                        {
                            SetEnvironmentGrid(null);
                        }
                    }
                }
                else
                {
                    SelectedDebugProfile = LaunchProfiles.Where(p => LaunchProfile.IsSameProfileName(p.Name, curProfileName)).Single();
                }
            }
            finally
            {
                PopIgnoreEvents();
                if(_firstSnapshotCompleteSource != null)
                {
                    _firstSnapshotCompleteSource.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// The initialization entry point for the page It also hooks into debug provider so that it can update when the profile changes
        /// </summary>
        protected void InitializePropertyPage()
        {
            if (_debugProfileProviderLink == null)
            {
                var debugProfilesBlock = new ActionBlock<ILaunchSettings>(
                async (profiles) =>
                {
                    if (_firstSnapshotCompleteSource == null)
                    {
                        await ProjectThreadingService.SwitchToUIThread();
                    }
                    InitializeDebugTargetsCore(profiles);
                });

                var profileProvider = GetDebugProfileProvider();
                _debugProfileProviderLink = profileProvider.SourceBlock.LinkTo(
                    debugProfilesBlock,
                    linkOptions: new DataflowLinkOptions { PropagateCompletion = true });
                
                // We need to get the set of UI providers, if any.
                InitializeUIProviders();
            }
        }

        /// <summary>
        /// initializes the collection of UI providers.
        /// </summary>
        private void InitializeUIProviders()
        {
            // We need to get the set of UI providers, if any.
            _uiProviders = new OrderPrecedenceImportCollection<ILaunchSettingsUIProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, UnconfiguredProject);
            var uiProviders = GetUIProviders();
            foreach (var uiProvider in uiProviders)
            {
                _uiProviders.Add(uiProvider);
            }
        }

        /// <summary>
        /// Gets the UI providers
        /// </summary>
        protected virtual IEnumerable<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>> GetUIProviders()
        {
            return UnconfiguredProject.Services.ExportProvider.GetExports<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>();
        }

        /// <summary>
        /// Returns the active UI provider for the current selected launchType or mull if no selection or the selected item
        /// does not have a provider installed
        /// </summary>
        private ILaunchSettingsUIProvider ActiveProvider
        {
            get
            {
                if(SelectedLaunchType == null)
                {
                    return null;
                }

                var activeProvider =  _uiProviders.FirstOrDefault((p) => string.Equals(p.Value.CommandName, SelectedLaunchType.CommandName, StringComparison.OrdinalIgnoreCase));
                return activeProvider?.Value;
            }
        }

        public override System.Threading.Tasks.Task Initialize()
        {
            // Initialize the page
            InitializePropertyPage();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Called when then the user saves the form.
        /// </summary>
        public async override Task<int> Save()
        {
            if(HasErrors)
            {
                throw new Exception(PropertyPageResources.ErrorsMustBeCorrectedPriorToSaving);
            }

            await SaveLaunchSettings().ConfigureAwait(false);

            return VSConstants.S_OK;
        }
                
                
        private ICommand _addEnironmentVariableRowCommand;
        public ICommand AddEnvironmentVariableRowCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _addEnironmentVariableRowCommand, () =>
                    new DelegateCommand((state) =>
                    {
                        NameValuePair newRow = new NameValuePair(PropertyPageResources.EnvVariableNameWatermark, PropertyPageResources.EnvVariableValueWatermark, EnvironmentVariables);
                        EnvironmentVariables.Add(newRow);
                        EnvironmentVariablesRowSelectedIndex = EnvironmentVariables.Count - 1;
                        //Raise event to focus on 
                        if (FocusEnvironmentVariablesGridRow != null)
                        {
                            FocusEnvironmentVariablesGridRow.Invoke(this, EventArgs.Empty);
                        }
                    }));
            }
        }

        private ICommand _removeEnvironmentVariableRowCommand;
        public ICommand RemoveEnvironmentVariableRowCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _removeEnvironmentVariableRowCommand, () =>
                    new DelegateCommand(state =>
                    {
                        int oldIndex = EnvironmentVariablesRowSelectedIndex;
                        EnvironmentVariables.RemoveAt(EnvironmentVariablesRowSelectedIndex);
                        EnvironmentVariablesValid = true;
                        EnvironmentVariablesRowSelectedIndex = (oldIndex == EnvironmentVariables.Count) ? oldIndex - 1 : oldIndex;
                    }));
            }
        }

        private ICommand _browseDirectoryCommand;
        public ICommand BrowseDirectoryCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _browseDirectoryCommand, () =>
                    new DelegateCommand(state =>
                    {
                        using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                        {
                            var folder = WorkingDirectory;
                            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                            {
                                dialog.SelectedPath = folder;
                            }
                            var result = dialog.ShowDialog();
                            if (result == System.Windows.Forms.DialogResult.OK)
                            {
                                WorkingDirectory = dialog.SelectedPath.ToString();
                            }
                        }
                    }));
            }
        }

        private ICommand _browseExecutableCommand;
        public ICommand BrowseExecutableCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _browseExecutableCommand, () =>
                    new DelegateCommand(state =>
                    {
                        using (var dialog = new System.Windows.Forms.OpenFileDialog())
                        {
                            var file = ExecutablePath;
                            if ((file.IndexOfAny(Path.GetInvalidPathChars()) == -1) && Path.IsPathRooted(file))
                            {
                                dialog.InitialDirectory = Path.GetDirectoryName(file);
                                dialog.FileName = file;
                            }
                            dialog.Multiselect = false;
                            dialog.Filter = _executableFilter;
                            var result = dialog.ShowDialog();
                            if (result == System.Windows.Forms.DialogResult.OK)
                            {
                                ExecutablePath = dialog.FileName.ToString();
                            }
                        }
                    }));
            }
        }
        
        public bool NewProfileEnabled
        {
            get
            {
                return LaunchProfiles != null;
            }
        }

        private ICommand _newProfileCommand;
        public ICommand NewProfileCommand
        {
            get
            {
                 return LazyInitializer.EnsureInitialized(ref _newProfileCommand, () =>
                  new DelegateCommand(state =>
                  {
                      var dialog = new GetProfileNameDialog(UnconfiguredProject.Services.ExportProvider.GetExportedValue<SVsServiceProvider>(),
                                                             ProjectThreadingService, 
                                                             GetNewProfileName(), 
                                                             IsNewProfileNameValid);
                      if (dialog.ShowModal() == true)
                      {
                          CreateProfile(dialog.ProfileName, ProfileCommandNames.Executable);
                      }
                  })); 
            }
        }

        public bool DeleteProfileEnabled
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return false;
                }

                return true;
            }
        }

        private ICommand _deleteProfileCommand;
        public ICommand DeleteProfileCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _deleteProfileCommand, () =>
                    new DelegateCommand(state =>
                    {
                        var profileToRemove = SelectedDebugProfile;
                        SelectedDebugProfile = null;

                        CurrentLaunchSettings.Profiles.Remove(profileToRemove);
                        LaunchProfiles.Remove(profileToRemove);
                        
                        SelectedDebugProfile = LaunchProfiles.Count > 0 ? LaunchProfiles[0] : null;
                        NotifyProfileCollectionChanged();
                    }));
            }
        }

        internal void CreateProfile(string name, string commandName)
        {
            var profile = new WritableLaunchProfile() { Name = name, CommandName = commandName };
            CurrentLaunchSettings.Profiles.Add(profile);
            LaunchProfiles.Add(profile);

            NotifyProfileCollectionChanged();
            
            // Fire a property changed so we can get the page to be dirty when we add a new profile
            OnPropertyChanged("_NewProfile");
            SelectedDebugProfile = profile;
        }

        internal bool IsNewProfileNameValid(string name)
        {
            return LaunchProfiles.Where(
                profile => LaunchProfile.IsSameProfileName(profile.Name, name)).Count() == 0;
        }

        internal string GetNewProfileName()
        {
            for (int i = 1; i < int.MaxValue; i++)
            {
                string profileName = string.Format("{0}{1}", PropertyPageResources.NewProfileSeedName, i.ToString());
                if (IsNewProfileNameValid(profileName))
                {
                    return profileName;
                }
            }

            return string.Empty;
        }
        
        /// <summary>
        /// Called after every profile change to update the list of launch types based on the following:
        /// 
        ///     The list of UI providers as each provider provides a name
        ///     The command name in the profile if it doesn't match one of the existing providers.
        ///     
        /// </summary>
        private List<LaunchType> _providerLaunchTypes;
        private void UpdateLaunchTypes()
        {
            // Populate the set of unique launch types from the list of providers since there can be duplicates with different priorities. However,
            // the command name will be the same so we can grab the first one for the purposes of populating the list
            if(_providerLaunchTypes == null)
            {
                _providerLaunchTypes =  new List<LaunchType>();
                foreach(var provider in _uiProviders)
                {
                    if(_providerLaunchTypes.FirstOrDefault((lt) => lt.CommandName.Equals(provider.Value.CommandName)) == null)
                    {
                        _providerLaunchTypes.Add(new LaunchType() { CommandName = provider.Value.CommandName, Name = provider.Value.FriendlyName});
                    }
                }
            }

            var selectedProfile = SelectedDebugProfile;
            LaunchType selectedLaunchType = null;

            _launchTypes = new List<LaunchType>();
            if (selectedProfile != null)
            {
                _launchTypes.AddRange(_providerLaunchTypes);

                selectedLaunchType = _launchTypes.FirstOrDefault((launchType) => string.Equals(launchType.CommandName, selectedProfile.CommandName));
                if(selectedLaunchType == null)
                {
                    selectedLaunchType = new LaunchType() { CommandName = selectedProfile.CommandName, Name = selectedProfile.CommandName};
                    _launchTypes.Insert(0, selectedLaunchType);
                }
            }

            // Need to notify the list has changed prior to changing the selected one
            OnPropertyChanged(nameof(LaunchTypes));

            SelectedLaunchType = selectedLaunchType;
        }
        
        private void EnvironmentVariables_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // cause the property page to be dirtied when a row is added or removed
            OnPropertyChanged("EnvironmentVariables_Contents");
        }

        private void DebugPageViewModel_EnvironmentVariables_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // cause the property page to be dirtied when a cell is updated
            OnPropertyChanged("EnvironmentVariables_Contents");
        }

        /// <summary>
        /// Overridden to do cleanup
        /// </summary>
        public override void ViewModelDetached()
        {
            if (_debugProfileProviderLink != null)
            {
                _debugProfileProviderLink.Dispose();
                _debugProfileProviderLink = null;
            }

            PropertyChanged -= ViewModel_PropertyChanged;
        }
        
        ILaunchSettingsProvider _launchSettingsProvider;
        protected virtual ILaunchSettingsProvider GetDebugProfileProvider()
        {
            if(_launchSettingsProvider == null)
            {
                _launchSettingsProvider = UnconfiguredProject.Services.ExportProvider.GetExportedValue<ILaunchSettingsProvider>();
            }

            return _launchSettingsProvider;
        }

        /// <summary>
        /// Called by the currently active control when errors within the control have changed
        /// </summary>
        private void OnCustomUIErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            ErrorsChanged?.Invoke(this, e);

            OnPropertyChanged(nameof(DoesNotHaveErrors));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            return null;
        }

        /// <summary>
        /// We are considered in error if we have errors, or the currently active control has errors
        /// </summary>
        public bool HasErrors
        {
            get
            {
                if (ActiveProvider?.CustomUI?.DataContext is INotifyDataErrorInfo notifyDataError)
                {
                    return notifyDataError.HasErrors;
                }

                return false;
            }
        }

        public bool DoesNotHaveErrors
        {
            get
            {
                return !HasErrors;
            }
        }

        public class LaunchType
        {
            public string CommandName { get; set; }
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                if (obj is LaunchType oth)
                {
                    return CommandName.Equals(oth.CommandName);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return CommandName.GetHashCode();
            }
        }
    }

    public static class ProfileCommandNames
    {
        public const string Project = "Project";
        public const string IISExpress = "IISExpress";
        public const string Executable = "Executable";
        public const string NoAction = "NoAction";
    }
}
