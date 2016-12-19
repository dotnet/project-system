// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Input;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class DebugPageViewModel : PropertyPageViewModel
    {
        private readonly string _executableFilter = String.Format("{0} (*.exe)|*.exe|{1} (*.*)|*.*", PropertyPageResources.ExecutableFiles, PropertyPageResources.AllFiles);
        private IDisposable _debugProfileProviderLink;
        private bool _useTaskFactory = true;
        
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

        public event EventHandler ClearEnvironmentVariablesGridError;
        public event EventHandler FocusEnvironmentVariablesGridRow;
        public DebugPageViewModel()
        {

        }
        // for unit testing
        internal DebugPageViewModel(bool useTaskFactory,UnconfiguredProject unconfiguredProject)
        {
            _useTaskFactory = useTaskFactory;
            UnconfiguredProject = unconfiguredProject;
        }
        
        public string SelectedCommandName
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return string.Empty;
                }

                return SelectedDebugProfile.CommandName;
            }
            set
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile.CommandName != value)
                {
                    SelectedDebugProfile.CommandName = value;
                    OnPropertyChanged(nameof(SelectedCommandName));
                }
            }
        }

        private ObservableCollection<string> _commandNames;
        public ObservableCollection<string> CommandNames
        {
            get
            {
                return _commandNames;
            }
            set
            {
                OnPropertyChanged(ref _commandNames, value);
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
                                ExecutablePath = String.Empty;
                            }
                        }
                        else
                        {
                            ExecutablePath = null;
                        }

                        OnPropertyChanged(nameof(IsExecutable));
                        OnPropertyChanged(nameof(IsProject));
                    }
                }
            }
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

        public bool IsExecutable
        {
            get
            {
                if (SelectedLaunchType == null)
                {
                    return false;
                }

                return SelectedLaunchType.CommandName == ProfileCommandNames.Executable;
            }
        }

        public bool IsProject
        {
            get
            {
                if (SelectedLaunchType == null)
                {
                    return false;
                }

                return SelectedLaunchType.CommandName == ProfileCommandNames.Project;
            }
        }
        
        public bool IsProfileSelected
        {
            get
            {
                return SelectedDebugProfile != null;
            }
        }

        private ObservableCollection<LaunchProfile> _debugProfiles;
        public ObservableCollection<LaunchProfile> DebugProfiles
        {
            get
            {
                return _debugProfiles;
            }
            set
            {
                var oldProfiles = _debugProfiles;
                if (OnPropertyChanged(ref _debugProfiles, value))
                {
                    if (oldProfiles != null)
                    {
                        oldProfiles.CollectionChanged -= DebugProfiles_CollectionChanged;
                    }
                    if (_debugProfiles != null)
                    {
                        _debugProfiles.CollectionChanged += DebugProfiles_CollectionChanged;
                    }
                    OnPropertyChanged(nameof(HasProfiles));
                    OnPropertyChanged(nameof(NewProfileEnabled));
                }
            }
        }

        private void DebugProfiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasProfiles));
        }
        
        public bool HasProfiles
        {
            get
            {
                return _debugProfiles != null && _debugProfiles.Count > 0;
            }
        }

        private LaunchProfile _selectedDebugProfile;
        public LaunchProfile SelectedDebugProfile
        {
            get
            {
                return _selectedDebugProfile;
            }
            set
            {
                if (_selectedDebugProfile != value)
                {
                    var oldProfile = _selectedDebugProfile;
                    _selectedDebugProfile = value;
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

        protected virtual void NotifySelectedChanged(LaunchProfile oldProfile)
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
                OnPropertyChanged(nameof(SelectedCommandName));
                
                SetLaunchType();

                OnPropertyChanged(nameof(IsExecutable));
                OnPropertyChanged(nameof(IsProject));
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
            if (EnvironmentVariables != null && EnvironmentVariables.Count > 0)
            {
                SelectedDebugProfile.MutableEnvironmentVariables = EnvironmentVariables.CreateDictionary();
            }
            else if (SelectedDebugProfile != null)
            {
                SelectedDebugProfile.MutableEnvironmentVariables = null;
            }
            var globalSettings = provider.CurrentSnapshot.GlobalSettings;
            await provider.UpdateAndSaveSettingsAsync(new LaunchSettings(DebugProfiles, globalSettings, SelectedDebugProfile?.Name)).ConfigureAwait(false);
        }

        private void SetEnvironmentGrid(LaunchProfile oldProfile)
        {
            if (EnvironmentVariables != null && oldProfile != null)
            {
                if (_environmentVariables.Count > 0)
                {
                    oldProfile.MutableEnvironmentVariables = EnvironmentVariables.CreateDictionary();
                }
                else
                {
                    oldProfile.MutableEnvironmentVariables = null;
                }
                EnvironmentVariables.ValidationStatusChanged -= EnvironmentVariables_ValidationStatusChanged;
                EnvironmentVariables.CollectionChanged -= EnvironmentVariables_CollectionChanged;
                ((INotifyPropertyChanged)EnvironmentVariables).PropertyChanged -= DebugPageViewModel_EnvironmentVariables_PropertyChanged;
            }

            if (SelectedDebugProfile != null && SelectedDebugProfile.MutableEnvironmentVariables != null)
            {
                EnvironmentVariables = SelectedDebugProfile.MutableEnvironmentVariables.CreateList();
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
        /// will also cause this to change as the active profile is stored in profiles snaphost.
        /// </summary>
        internal virtual void InitializeDebugTargetsCore(ILaunchSettings profiles)
        {
            bool profilesChanged = true;
            bool IISSettingsChanged = true;

            // Since this get's reentered if the user saves or the user switches active profiles.
            if (DebugProfiles != null)
            {
                profilesChanged = profiles.ProfilesAreDifferent(DebugProfiles.Select(p => (ILaunchProfile)p).ToList());
                if (!profilesChanged && !IISSettingsChanged)
                {
                    return;
                }
            }

            try
            {
                // This should never change the dirty state
                PushIgnoreEvents();

                if (profilesChanged)
                {
                    // Remember the current selection
                    string curProfileName = SelectedDebugProfile?.Name;

                    // Load debug profiles
                    var debugProfiles = new ObservableCollection<LaunchProfile>();

                    foreach (var profile in profiles.Profiles)
                    {
                        // Don't show the dummy NoAction profile
                        if (profile.CommandName != ProfileCommandNames.NoAction)
                        {
                            var newProfile = new LaunchProfile(profile);
                            debugProfiles.Add(newProfile);
                        }
                    }
                                        
                    DebugProfiles = debugProfiles;

                    // If we have a selection, we want to leave it as is
                    if (curProfileName == null || profiles.Profiles.FirstOrDefault(p => { return LaunchProfile.IsSameProfileName(p.Name, curProfileName); }) == null)
                    {
                        // Note that we have to be careful since the collection can be empty. 
                        if (!string.IsNullOrEmpty(profiles.ActiveProfile.Name))
                        {
                            SelectedDebugProfile = DebugProfiles.Where((p) => LaunchProfile.IsSameProfileName(p.Name, profiles.ActiveProfile.Name)).Single();
                        }
                        else
                        {
                            if (debugProfiles.Count > 0)
                            {
                                SelectedDebugProfile = debugProfiles[0];
                            }
                            else
                            {
                                SetEnvironmentGrid(null);
                            }
                        }
                    }
                    else
                    {
                        SelectedDebugProfile = DebugProfiles.Where((p) => LaunchProfile.IsSameProfileName(p.Name, curProfileName)).Single();
                    }
                }
                
            }
            finally
            {
                PopIgnoreEvents();
            }
        }

        /// <summary>
        /// Initializes from the set of debug targets. It also hooks into debug provider so that it can update when the profile changes
        /// </summary>
        protected virtual void InitializeDebugTargets()
        {
            if (_debugProfileProviderLink == null)
            {
                var debugProfilesBlock = new ActionBlock<ILaunchSettings>(
                async (profiles) =>
                {
                    if (_useTaskFactory)
                    {
                        await ProjectThreadingService.SwitchToUIThread();
                    }
                    InitializeDebugTargetsCore(profiles);
                });

                var profileProvider = GetDebugProfileProvider();
                _debugProfileProviderLink = profileProvider.SourceBlock.LinkTo(
                    debugProfilesBlock,
                    linkOptions: new DataflowLinkOptions { PropagateCompletion = true });
            }
        }
        public override System.Threading.Tasks.Task Initialize()
        {
            // Create the debug targets dropdown
            InitializeDebugTargets();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// Called when then the user saves the form.
        /// </summary>
        public async override Task<int> Save()
        {
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
                            if (!String.IsNullOrEmpty(folder) && Directory.Exists(folder))
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
        
        public string CopyHyperlinkText
        {
            get
            {
                // Can't use x:Static in xaml since our resources are internal.
                return PropertyPageResources.CopyHyperlinkText;
            }
        }

        public bool NewProfileEnabled
        {
            get
            {
                return _debugProfiles != null;
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
                        DebugProfiles.Remove(profileToRemove);
                        SelectedDebugProfile = DebugProfiles.Count > 0 ? DebugProfiles[0] : null;
                    }));
            }
        }

        internal LaunchProfile CreateProfile(string name, string commandName)
        {
            var profile = new LaunchProfile() { Name = name, CommandName = commandName };
            DebugProfiles.Add(profile);

            // Fire a property changed so we can get the page to be dirty when we add a new profile
            OnPropertyChanged("_NewProfile");
            SelectedDebugProfile = profile;
            return profile;
        }

        internal bool IsNewProfileNameValid(string name)
        {
            return DebugProfiles.Where(
                profile => LaunchProfile.IsSameProfileName(profile.Name, name)).Count() == 0;
        }

        internal string GetNewProfileName()
        {
            for (int i = 1; i < int.MaxValue; i++)
            {
                string profileName = String.Format("{0}{1}", PropertyPageResources.NewProfileSeedName, i.ToString());
                if (IsNewProfileNameValid(profileName))
                {
                    return profileName;
                }
            }

            return String.Empty;
        }
        
        private void SetLaunchType()
        {
            if (!IsProfileSelected)
            {
                _launchTypes = new List<LaunchType>();
            }
            else if (SelectedDebugProfile.CommandName == ProfileCommandNames.Executable ||
                     (SelectedDebugProfile.CommandName == ProfileCommandNames.IISExpress))
            {

                _launchTypes = LaunchType.GetExecutableOnlyLaunchTypes().ToList<LaunchType>();

            }
            else
            {
                _launchTypes = LaunchType.GetBuiltInLaunchTypes().ToList<LaunchType>();
            }

            OnPropertyChanged(nameof(LaunchTypes));

            // The selected launch type has to be tweaked for DotNet since in dotnet we don't want to support commands and yet user might have some commands 
            if (!IsProfileSelected)
            {
                SelectedLaunchType = null;
            }
            else
            {
                var selCommandName = SelectedDebugProfile.CommandName;
                SelectedLaunchType = LaunchType.GetAllLaunchTypes().Where(lt => lt.CommandName == selCommandName).SingleOrDefault();
            }
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
        }
        
        [ExcludeFromCodeCoverage]
        protected virtual ILaunchSettingsProvider GetDebugProfileProvider()
        {
            return UnconfiguredProject.Services.ExportProvider.GetExportedValue<ILaunchSettingsProvider>();
        }
                
        public class LaunchType
        {
            public String CommandName { get; set; }
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

            public static readonly LaunchType Executable = new LaunchType() { CommandName = ProfileCommandNames.Executable, Name = PropertyPageResources.ProfileKindExecutableName };
            public static readonly LaunchType Project = new LaunchType() { CommandName = ProfileCommandNames.Project, Name = PropertyPageResources.ProfileKindProjectName };
            public static readonly LaunchType IISExpress = new LaunchType() { CommandName = ProfileCommandNames.IISExpress, Name = PropertyPageResources.ProfileKindIISExpressName };

            public static LaunchType[] GetAllLaunchTypes()
            {
                return _allLaunchTypes;
            }
            
            public static LaunchType[] GetBuiltInLaunchTypes()
            {
                return  _builtInLaunchTypes;
            }
            
            public static LaunchType[] GetExecutableOnlyLaunchTypes()
            {
                return _executableOnlyLaunchType;
            }


            private static readonly LaunchType[] _allLaunchTypes = new LaunchType[] { Executable, IISExpress, Project };
            private static readonly LaunchType[] _builtInLaunchTypes = new LaunchType[] { Executable, Project };
            private static readonly LaunchType[] _executableOnlyLaunchType = new LaunchType[] { Executable, Project };
            
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
