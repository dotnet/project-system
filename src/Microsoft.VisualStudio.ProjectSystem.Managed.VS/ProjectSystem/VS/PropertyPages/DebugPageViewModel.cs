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
using System.Windows;
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
        // This holds the set of shared IIS settings. This affects windows\anon auth (shared across iis\iisExpress) and
        // the IIS and IIS Express bindings. 
        private IISSettings _currentIISSettings;
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
        private bool _useSpecificRuntime;
        public bool UseSpecificRuntime
        {
            get
            {
                return _useSpecificRuntime;
            }
            set
            {
                OnPropertyChanged(ref _useSpecificRuntime, value);
            }
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

        /// <summary>
        /// Where the data is bound depends on the currently active profile. IIS\IIS Express have specific binding information
        /// in the IISSEttings. However, profiles (web based) have an ApplicationUrl property
        /// </summary>
        public string ApplicationUrl
        {
            get
            {
                if (SelectedDebugProfile != null)
                {
                    if (SelectedDebugProfile.Kind == ProfileKind.IISExpress)
                    {
                        return _currentIISSettings?.IISExpressBindingData?.ApplicationUrl;
                    }
                    else if (SelectedDebugProfile.Kind == ProfileKind.IIS)
                    {
                        return _currentIISSettings?.IISBindingData?.ApplicationUrl;
                    }
                    else if (SelectedDebugProfile.IsWebServerCmdProfile)
                    {
                        return SelectedDebugProfile.ApplicationUrl;
                    }
                }
                return string.Empty;
            }
            set
            {
                if (value != ApplicationUrl)
                {
                    if (SelectedDebugProfile != null)
                    {
                        if (SelectedDebugProfile.Kind == ProfileKind.IISExpress)
                        {
                            if (_currentIISSettings != null && _currentIISSettings.IISExpressBindingData != null)
                            {
                                _currentIISSettings.IISExpressBindingData.ApplicationUrl = value;
                                OnPropertyChanged(nameof(ApplicationUrl));
                            }
                        }
                        else if (SelectedDebugProfile.Kind == ProfileKind.IIS)
                        {
                            if (_currentIISSettings != null && _currentIISSettings.IISBindingData != null)
                            {
                                _currentIISSettings.IISBindingData.ApplicationUrl = value;
                                OnPropertyChanged(nameof(ApplicationUrl));
                            }
                        }
                        else if (SelectedDebugProfile.IsWebServerCmdProfile)
                        {
                            SelectedDebugProfile.ApplicationUrl = value;
                            OnPropertyChanged(nameof(ApplicationUrl));
                        }
                    }
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
                if (OnPropertyChanged(ref _selectedLaunchType, value))
                {
                    // Existing commands are shown in the UI as project
                    // since that is what is run when that command is selected. However, we don't want to just update the actual
                    // profile to this value - we want to treat them as equivalent.
                    if (_selectedLaunchType != null && !IsEquivalentProfileKind(SelectedDebugProfile, _selectedLaunchType.Kind))
                    {
                        SelectedDebugProfile.Kind = _selectedLaunchType.Kind;
                        if (_selectedLaunchType.Kind == ProfileKind.Executable)
                        {
                            ExecutablePath = String.Empty;
                            SelectedCommandName = null;
                            
                        }
                        else if (_selectedLaunchType.Kind == ProfileKind.IISExpress)
                        {
                            ExecutablePath = String.Empty;
                            SelectedCommandName = LaunchSettingsProvider.IISExpressProfileCommandName;
                            HasLaunchOption = true;
                        }
                        else
                        {
                            SelectedCommandName = CommandNames[0];
                            ExecutablePath = null;
                        }

                        OnPropertyChanged(nameof(IsCommand));
                        OnPropertyChanged(nameof(IsExecutable));
                        OnPropertyChanged(nameof(IsProject));
                        OnPropertyChanged(nameof(IsIISExpress));
                        OnPropertyChanged(nameof(ShowVersionSelector));
                    }
                }
            }
        }

        /// <summary>
        /// Existing commands is shown in the UI as project
        /// since that is what is run when that command is selected. However, we don't want tojust update the actual
        /// profile to this value - we want to treat them as equivalent.
        ///</summary>
        private bool IsEquivalentProfileKind(LaunchProfile profile, ProfileKind kind)
        {

            if (kind == ProfileKind.Project)
            {
                return profile.Kind == ProfileKind.Project ||
                       profile.Kind == ProfileKind.BuiltInCommand ||
                       profile.Kind == ProfileKind.CustomizedCommand;
            }

            return profile.Kind == kind;
        }

        public bool IsBuiltInProfile
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return false;
                }

                return SelectedDebugProfile.Kind == ProfileKind.BuiltInCommand;
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

                return SelectedLaunchType.Kind == ProfileKind.Executable;
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

                return SelectedLaunchType.Kind == ProfileKind.Project;
            }
        }

        public bool IsCommand
        {
            get
            {
                if (SelectedLaunchType == null)
                {
                    return false;
                }

                return SelectedLaunchType.Kind == ProfileKind.BuiltInCommand || SelectedLaunchType.Kind == ProfileKind.CustomizedCommand;
            }
        }

        public bool IsProfileSelected
        {
            get
            {
                return SelectedDebugProfile != null;
            }
        }

        public bool IsIISExpress
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return false;
                }

                return SelectedDebugProfile.Kind == ProfileKind.IISExpress;
            }
        }

        public bool IsIISOrIISExpress
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return false;
                }

                return SelectedDebugProfile.Kind == ProfileKind.IIS || SelectedDebugProfile.Kind == ProfileKind.IISExpress;
            }
        }

        public bool IsWebProfile
        {
            get
            {
                if (!IsProfileSelected)
                {
                    return false;
                }

                return IsIISOrIISExpress;
            }
        }

        public bool ShowVersionSelector
        {
            get
            {
                return !IsExecutable;
            }
        }
        public bool IsCustomType
        {
            get
            {
                if (SelectedLaunchType == null || SelectedDebugProfile == null)
                {
                    return false;
                }

                return SelectedLaunchType.Kind == ProfileKind.CustomizedCommand ||
                       SelectedLaunchType.Kind == ProfileKind.Executable ||
                       SelectedLaunchType.Kind == ProfileKind.IIS ||
                       (SelectedDebugProfile.Kind == ProfileKind.IISExpress && !SelectedDebugProfile.IsDefaultIISExpressProfile) ||
                       (SelectedDebugProfile.Kind == ProfileKind.Project && !LaunchProfile.IsSameProfileName(SelectedDebugProfile.Name, UnconfiguredProject.FullPath));
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

        public bool SSLEnabled
        {
            get
            {
                if (_currentIISSettings != null && SelectedDebugProfile != null)
                {
                    if (SelectedDebugProfile.Kind == ProfileKind.IISExpress && _currentIISSettings.IISExpressBindingData != null)
                    {
                        return _currentIISSettings.IISExpressBindingData.SSLPort != 0;
                    }
                    else if (SelectedDebugProfile.Kind == ProfileKind.IIS && _currentIISSettings.IISBindingData != null)
                    {
                        return _currentIISSettings.IISBindingData.SSLPort != 0;
                    }
                }
                return false;
            }
            set
            {
                // When transitioning to enabled we want to go get an ssl port. Of course when loading (ignore events is true) we don't 
                // want to do this.
                if (value != SSLEnabled && !IgnoreEvents)
                {
                    ServerBinding binding = null;
                    if (_currentIISSettings != null)
                    {
                        if (SelectedDebugProfile.Kind == ProfileKind.IISExpress && _currentIISSettings.IISExpressBindingData != null)
                        {
                            binding = _currentIISSettings.IISExpressBindingData;
                        }
                        else if (SelectedDebugProfile.Kind == ProfileKind.IIS && _currentIISSettings.IISBindingData != null)
                        {
                            binding = _currentIISSettings.IISBindingData;
                        }
                    }
                    if (binding != null)
                    {
                        // When setting we need to configure the port
                        if (value == true)
                        {
                            // If we are already have a port (say the guy was enabling\disabing over and over), use that (nothing to do here)
                            if (string.IsNullOrWhiteSpace(SSLUrl))
                            {
                                ValidateApplicationUrl();

                                // No existing value. Go get one and set the url
                                // First we must validate the ApplicationUrl is valid. W/O it we don't know the host header
                                if (SelectedDebugProfile.Kind == ProfileKind.IISExpress)
                                {
                                    // Get the SSLPort provider
                                    var sslPortProvider = GetSSLPortProvider();
                                    if (sslPortProvider != null)
                                    {
                                        binding.SSLPort = sslPortProvider.GetAvailableSSLPort(ApplicationUrl);
                                    }
                                    else
                                    {
                                        // Just set it to a default iis express value
                                        binding.SSLPort = 44300;
                                    }
                                }
                                else
                                {
                                    //For IIS use 443 as the binding
                                    binding.SSLPort = 443;
                                }
                            }
                            else
                            {
                                binding.SSLPort = new Uri(SSLUrl).Port;
                            }
                        }
                        else
                        {
                            // Just clear the port. We don't clear the SSL url so that it persists (disabled) until we update.
                            binding.SSLPort = 0;
                        }
                    }
                }
                OnPropertyChanged(nameof(SSLEnabled));
                OnPropertyChanged(nameof(SSLUrl));
            }
        }

        /// <summary>
        /// This property is synthesized from the SSLPort changing.
        /// </summary>
        public string SSLUrl
        {
            get
            {
                int sslPort = 0;
                if (_currentIISSettings != null && SelectedDebugProfile != null)
                {
                    if (SelectedDebugProfile.Kind == ProfileKind.IISExpress && _currentIISSettings.IISExpressBindingData != null)
                    {
                        sslPort = _currentIISSettings.IISExpressBindingData.SSLPort;
                    }
                    else if (SelectedDebugProfile.Kind == ProfileKind.IIS && _currentIISSettings.IISBindingData != null)
                    {
                        sslPort = _currentIISSettings.IISBindingData.SSLPort;
                    }

                    if (sslPort != 0)
                    {
                        try
                        {
                            // Application url could be bad so we need to protect ourself.
                            if (!string.IsNullOrWhiteSpace(ApplicationUrl))
                            {
                                return UriUtilities.MakeSecureUrl(ApplicationUrl, sslPort);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// IIS\IISExpress specific property (shared between them) and stored in  _currentIISSetings 
        /// </summary>
        public bool AnonymousAuthenticationEnabled
        {
            get
            {
                if (_currentIISSettings == null)
                {
                    return false;
                }
                return _currentIISSettings.AnonymousAuthentication;
            }
            set
            {
                if (_currentIISSettings != null && _currentIISSettings.AnonymousAuthentication != value)
                {
                    _currentIISSettings.AnonymousAuthentication = value;
                    OnPropertyChanged(nameof(AnonymousAuthenticationEnabled));
                }
            }
        }

        /// <summary>
        /// IIS\IISExpress specific property (shared between them) and stored in  _currentIISSetings 
        /// </summary>
        public bool WindowsAuthenticationEnabled
        {
            get
            {
                if (_currentIISSettings == null)
                {
                    return false;
                }
                return _currentIISSettings.WindowsAuthentication;
            }
            set
            {
                if (_currentIISSettings != null && _currentIISSettings.WindowsAuthentication != value)
                {
                    _currentIISSettings.WindowsAuthentication = value;
                    OnPropertyChanged(nameof(WindowsAuthenticationEnabled));
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
                OnPropertyChanged(nameof(IsBuiltInProfile));
                OnPropertyChanged(nameof(IsIISExpress));
                OnPropertyChanged(nameof(IsIISOrIISExpress));
                OnPropertyChanged(nameof(IsWebProfile));
                OnPropertyChanged(nameof(CommandLineArguments));
                OnPropertyChanged(nameof(ExecutablePath));
                OnPropertyChanged(nameof(LaunchPage));
                OnPropertyChanged(nameof(HasLaunchOption));
                OnPropertyChanged(nameof(WorkingDirectory));
                OnPropertyChanged(nameof(SelectedCommandName));

                OnPropertyChanged(nameof(SSLEnabled));
                OnPropertyChanged(nameof(SSLUrl));
                OnPropertyChanged(nameof(ApplicationUrl));
                OnPropertyChanged(nameof(WindowsAuthenticationEnabled));
                OnPropertyChanged(nameof(AnonymousAuthenticationEnabled));


                SetLaunchType();

                OnPropertyChanged(nameof(IsCustomType));
                OnPropertyChanged(nameof(IsCommand));
                OnPropertyChanged(nameof(IsExecutable));
                OnPropertyChanged(nameof(IsProject));
                OnPropertyChanged(nameof(IsProfileSelected));
                OnPropertyChanged(nameof(DeleteProfileEnabled));
                OnPropertyChanged(nameof(ShowVersionSelector));

                if (oldProfile != null && !UseSpecificRuntime)
                {
                    oldProfile.SDKVersion = null;
                }

                UseSpecificRuntime = true;
                
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
            
            //await provider.UpdateAndSaveSettingsAsync(new LaunchSettings(DebugProfiles, GetIISSettings(), SelectedDebugProfile != null ? SelectedDebugProfile.Name : null));
            await provider.UpdateAndSaveSettingsAsync(new LaunchSettings(DebugProfiles, null, SelectedDebugProfile != null ? SelectedDebugProfile.Name : null)).ConfigureAwait(false);

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
                IISSettingsChanged = profiles.IISSettingsAreDifferent(GetIISSettings());
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
                    string curProfileName = SelectedDebugProfile == null ? null : SelectedDebugProfile.Name;

                    // Load debug profiles
                    var debugProfiles = new ObservableCollection<LaunchProfile>();

                    foreach (var profile in profiles.Profiles)
                    {
                        // Don't show the dummy NoAction profile
                        if (profile.Kind != ProfileKind.NoAction)
                        {
                            var newProfile = new LaunchProfile(profile);
                            debugProfiles.Add(newProfile);
                        }
                    }

                    CommandNames = new ObservableCollection<string>(debugProfiles.Where(p => p.Kind == ProfileKind.BuiltInCommand).Select(pr => pr.CommandName));

                    DebugProfiles = debugProfiles;

                    // If we have a selection, we want to leave it as is
                    if (curProfileName == null || profiles.Profiles.FirstOrDefault(p => { return LaunchProfile.IsSameProfileName(p.Name, curProfileName); }) == null)
                    {
                        // Note that we have to be careful since the collection can be empty. 
                        if (!string.IsNullOrEmpty(profiles.ActiveProfileName))
                        {
                            SelectedDebugProfile = DebugProfiles.Where((p) => LaunchProfile.IsSameProfileName(p.Name, profiles.ActiveProfileName)).Single();
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
                if (IISSettingsChanged)
                {
                    InitializeIISSettings(profiles.IISSettings);
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
        
        public async override System.Threading.Tasks.Task Initialize()
        {
            // Need to set whether this is a web project or not since other parts of the code will use the cached value
            await IsWebProjectAsync().ConfigureAwait(false);
                        
            // Create the debug targets dropdown
            InitializeDebugTargets();
        }

        /// <summary>
        /// Called whenever new settings are retrieved. Note that the controls which are affected depend heavily on the
        /// currently selected profile.
        /// </summary>
        private void InitializeIISSettings(IIISSettings iisSettings)
        {
            if (iisSettings == null)
            {
                _currentIISSettings = null;
                return;
            }

            _currentIISSettings = IISSettings.FromIIISSettings(iisSettings);

            OnPropertyChanged(nameof(ApplicationUrl));
            OnPropertyChanged(nameof(SSLEnabled));
            OnPropertyChanged(nameof(SSLUrl));
            OnPropertyChanged(nameof(WindowsAuthenticationEnabled));
            OnPropertyChanged(nameof(AnonymousAuthenticationEnabled));
        }

        /// <summary>
        /// Called when then the user saves the form.
        /// </summary>
        public async override Task<int> Save()
        {

            // For web projects, we need to validate the settings -especially the appUrl from which everything else hangs.
            /* Uncomment the validation once IISsettings is working
             * if (ContainsProfileKind(ProfileKind.IISExpress))
            {
                if (_currentIISSettings == null || _currentIISSettings.IISExpressBindingData == null || string.IsNullOrEmpty(_currentIISSettings.IISExpressBindingData.ApplicationUrl))
                {
                    throw new Exception(PropertyPageResources.IISExpressMissingAppUrl);
                }
                try
                {
                    Uri appUri = new Uri(_currentIISSettings.IISExpressBindingData.ApplicationUrl, UriKind.Absolute);
                    if (appUri.Port < 1024)
                    {
                        throw new Exception(PropertyPageResources.AdminRequiredForPort);
                    }
                }
                catch (UriFormatException ex)
                {
                    throw new Exception(string.Format(PropertyPageResources.InvalidIISExpressAppUrl, ex.Message));
                }
            }*/
            if (ContainsProfileKind(ProfileKind.IIS))
            {
                if (_currentIISSettings == null || _currentIISSettings.IISBindingData == null || string.IsNullOrEmpty(_currentIISSettings.IISBindingData.ApplicationUrl))
                {
                    throw new Exception(PropertyPageResources.IISMissingAppUrl);
                }
                try
                {
                    Uri appUri = new Uri(_currentIISSettings.IISBindingData.ApplicationUrl, UriKind.Absolute);
                }
                catch (UriFormatException ex)
                {
                    throw new Exception(string.Format(PropertyPageResources.InvalidIISAppUrl, ex.Message));
                }
            }

            // Persist the settings. The change in IIS Express settings will be tracked by the WebStateManager which will 
            // configure the IIS Express server as needed.
            await SaveLaunchSettings().ConfigureAwait(false);

            return VSConstants.S_OK;
        }

        /// <summary>
        ///  Helper to determine if an IIS Express profile is defined
        /// </summary>
        private void ValidateApplicationUrl()
        {
            if (string.IsNullOrEmpty(ApplicationUrl))
            {
                throw new Exception(PropertyPageResources.IISExpressMissingAppUrl);
            }
            try
            {
                Uri appUri = new Uri(ApplicationUrl, UriKind.Absolute);
                if (appUri.Port < 1024)
                {
                    throw new Exception(PropertyPageResources.AdminRequiredForPort);
                }
            }
            catch (UriFormatException ex)
            {
                throw new Exception(string.Format(PropertyPageResources.InvalidAppUrl, ex.Message));
            }
        }

        /// <summary>
        ///  Helper to determine if a particular profile type is defined
        /// </summary>
        private bool ContainsProfileKind(ProfileKind kind)
        {
            return DebugProfiles != null && DebugProfiles.FirstOrDefault(p => p.Kind == kind) != null;
        }

        /// <summary>
        /// Helper to get the IIS Settings
        /// </summary>
        private IISSettingsProfile GetIISSettings()
        {
            if (_currentIISSettings != null)
            {
                return new IISSettingsProfile(_currentIISSettings);
            }

            return null;
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
                            if (Path.IsPathRooted(file))
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
        private ICommand _copySSLUrlCommand;
        public ICommand CopySSLUrlCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _copySSLUrlCommand, () =>
                    new DelegateCommand((state) =>
                    {
                        Clipboard.SetText(SSLUrl);
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
                          CreateProfile(dialog.ProfileName, ProfileKind.Executable);
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

                return !IsBuiltInProfile && !SelectedDebugProfile.IsDefaultIISExpressProfile;
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

        internal LaunchProfile CreateProfile(string name, ProfileKind kind)
        {
            var profile = new LaunchProfile() { Name = name, Kind = kind };
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

        /// <summary>
        /// Helper to determine if this is a web project or not. We cache it so we can get it in a non
        /// async way from the UI.
        /// </summary>
        public bool IsWebProject { get; private set; }

        private void SetLaunchType()
        {
            if (!IsProfileSelected)
            {
                _launchTypes = new List<LaunchType>();
            }
            else if (SelectedDebugProfile.Kind == ProfileKind.CustomizedCommand || SelectedDebugProfile.Kind == ProfileKind.IIS ||
                     SelectedDebugProfile.Kind == ProfileKind.Executable ||
                     (SelectedDebugProfile.Kind == ProfileKind.IISExpress && !SelectedDebugProfile.IsDefaultIISExpressProfile))
            {
                // For customized commands, exe, IIS and non-built in IIS Express we allow the user to switch between them. Two cases, one where we have commands
                // and one where there are no commands defined in project.json
                if (CommandNames.Count > 0)
                {
                    _launchTypes = IsWebProject ? LaunchType.GetWebCustomizedLaunchTypes().ToList<LaunchType>() : LaunchType.GetCustomizedLaunchTypes().ToList<LaunchType>();
                }
                else
                {
                    _launchTypes = IsWebProject ? LaunchType.GetWebExecutableOnlyLaunchTypes().ToList<LaunchType>() : LaunchType.GetExecutableOnlyLaunchTypes().ToList<LaunchType>();
                }
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
                var selKind = SelectedDebugProfile.Kind;
                if ((selKind == ProfileKind.BuiltInCommand || selKind == ProfileKind.CustomizedCommand))
                {
                    selKind = ProfileKind.Project;
                }
                SelectedLaunchType = LaunchType.GetAllLaunchTypes().Where(lt => lt.Kind == selKind).SingleOrDefault();
            }
        }
        public virtual Task<bool> IsWebProjectAsync()
        {
            if (UnconfiguredProject.Capabilities != null && UnconfiguredProject.Capabilities.Contains("DotNetCoreWeb"))
            {
                IsWebProject = true;
            }
            return System.Threading.Tasks.Task.FromResult(IsWebProject);
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
        protected virtual IEnumerable<string> GetAvailableVersions()
        {
            return null;
        }

        [ExcludeFromCodeCoverage]
        protected virtual ILaunchSettingsProvider GetDebugProfileProvider()
        {
            return UnconfiguredProject.Services.ExportProvider.GetExportedValue<ILaunchSettingsProvider>();
        }

        [ExcludeFromCodeCoverage]
        protected virtual ISSLPortProvider GetSSLPortProvider()
        {
            try
            {
                return UnconfiguredProject.Services.ExportProvider.GetExportedValue<ISSLPortProvider>();
            }
            catch
            {
            }
            return null;
            
        }

        public class LaunchType
        {
            public ProfileKind Kind { get; set; }
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                LaunchType oth = obj as LaunchType;
                if (oth != null)
                {
                    return Kind.Equals(oth.Kind);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return Kind.GetHashCode();
            }

            public static readonly LaunchType CustomizedCommand = new LaunchType() { Kind = ProfileKind.CustomizedCommand, Name = PropertyPageResources.ProfileKindCommandName };
            public static readonly LaunchType BuiltInCommand = new LaunchType() { Kind = ProfileKind.BuiltInCommand, Name = PropertyPageResources.ProfileKindCommandName };
            public static readonly LaunchType Executable = new LaunchType() { Kind = ProfileKind.Executable, Name = PropertyPageResources.ProfileKindExecutableName };
            public static readonly LaunchType Project = new LaunchType() { Kind = ProfileKind.Project, Name = PropertyPageResources.ProfileKindProjectName };
            public static readonly LaunchType IISExpress = new LaunchType() { Kind = ProfileKind.IISExpress, Name = PropertyPageResources.ProfileKindIISExpressName };

            public static LaunchType[] GetAllLaunchTypes()
            {
                return _allLaunchTypes;
            }

            public static LaunchType[] GetWebCustomizedLaunchTypes()
            {
                return _webCustomizedLaunchTypes;
            }
            public static LaunchType[] GetCustomizedLaunchTypes()
            {
                return _customizedLaunchTypes;
            }

            public static LaunchType[] GetBuiltInLaunchTypes()
            {
                return  _builtInLaunchTypes;
            }

            public static LaunchType[] GetWebExecutableOnlyLaunchTypes()
            {
                return _webExecutableOnlyLaunchType;
            }

            public static LaunchType[] GetExecutableOnlyLaunchTypes()
            {
                return _executableOnlyLaunchType;
            }

            // billhie: IIS is disabled until RC2 once we have sorted out the hosting story so we don't define an IIS launch type. 
#if IISSUPPORT
            public static readonly LaunchType IIS = new LaunchType() { Kind = ProfileKind.IIS, Name = Resources.ProfileKindIISName };
            public static readonly LaunchType[] AllLaunchTypes = new LaunchType[] { Executable, IISExpress, IIS, Project };
            public static readonly LaunchType[] WebCustomizedLaunchTypes = new LaunchType[] { Executable, IISExpress, IIS, Project };
            public static readonly LaunchType[] WebExecutableOnlyLaunchType = new LaunchType[] { Executable, IISExpress, IIS, Project };
#else
            private static readonly LaunchType[] _allLaunchTypes = new LaunchType[] { Executable, IISExpress, Project };
            private static readonly LaunchType[] _webCustomizedLaunchTypes = new LaunchType[] { Executable, IISExpress, Project };
            private static readonly LaunchType[] _webExecutableOnlyLaunchType = new LaunchType[] { Executable, IISExpress, Project };
#endif
            private static readonly LaunchType[] _builtInLaunchTypes = new LaunchType[] { Executable, IISExpress, Project };
            private static readonly LaunchType[] _customizedLaunchTypes = new LaunchType[] { Executable, Project };
            private static readonly LaunchType[] _executableOnlyLaunchType = new LaunchType[] { Executable, Project };
            
        }
    }
}
