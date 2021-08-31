// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading.Tasks;
using DialogResult = System.Windows.Forms.DialogResult;
using Task = System.Threading.Tasks.Task;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal partial class DebugPageViewModel : PropertyPageViewModel, INotifyDataErrorInfo
    {
        private readonly string _executableFilter = string.Format("{0} (*.exe)|*.exe|{1} (*.*)|*.*", PropertyPageResources.ExecutableFiles, PropertyPageResources.AllFiles);
        private IDisposable _debugProfileProviderLink;
        private bool _removeEnvironmentVariablesRow;
        private bool _debugTargetsCoreInitialized;
        private bool _environmentVariablesValid = true;
        private int _environmentVariablesRowSelectedIndex = -1;
        private ILaunchSettingsProvider _launchSettingsProvider;
        private ObservableList<NameValuePair> _environmentVariables;
        private IProjectThreadingService _projectThreadingService;
        private List<LaunchType> _launchTypes;
        private List<LaunchType> _providerLaunchTypes;
        private LaunchType _selectedLaunchType;
        private OrderPrecedenceImportCollection<ILaunchSettingsUIProvider> _uiProviders;
        private readonly TaskCompletionSource _firstSnapshotCompleteSource;
        private ICommand _addEnvironmentVariableRowCommand;
        private ICommand _removeEnvironmentVariableRowCommand;
        private ICommand _browseDirectoryCommand;
        private ICommand _browseExecutableCommand;
        private ICommand _newProfileCommand;
        private ICommand _deleteProfileCommand;
        private ICommand _findRemoteMachineCommand;
        private IRemoteDebuggerAuthenticationService _remoteDebuggerAuthenticationService;

        public DebugPageViewModel()
        {
            // Hook into our own property changed event. This is solely to know when an active profile has been edited
            PropertyChanged += ViewModel_PropertyChanged;
        }

        // for unit testing
        internal DebugPageViewModel(TaskCompletionSource snapshotComplete, UnconfiguredProject project)
        {
            _firstSnapshotCompleteSource = snapshotComplete;
            Project = project;
            PropertyChanged += ViewModel_PropertyChanged;
        }

        public event EventHandler ClearEnvironmentVariablesGridError;
        public event EventHandler FocusEnvironmentVariablesGridRow;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private IWritableLaunchSettings CurrentLaunchSettings { get; set; }

        private IProjectThreadingService ProjectThreadingService
        {
            get
            {
                if (_projectThreadingService == null)
                {
                    IUnconfiguredProjectVsServices projectVsServices = Project.Services.ExportProvider.GetExportedValue<IUnconfiguredProjectVsServices>();
                    _projectThreadingService = projectVsServices.ThreadingService;
                }

                return _projectThreadingService;
            }
        }

        private IRemoteDebuggerAuthenticationService RemoteDebuggerAuthenticationService
        {
            get
            {
                if (_remoteDebuggerAuthenticationService == null)
                {
                    _remoteDebuggerAuthenticationService = Project.Services.ExportProvider.GetExportedValue<IRemoteDebuggerAuthenticationService>();
                }

                return _remoteDebuggerAuthenticationService;
            }
        }

        /// <summary>
        /// This is here so that we can clear the in-memory status of the active profile if it has been edited. This is
        /// so that the profile, and hence the user's customizations, will be saved to disk
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IgnoreEvents)
            {
                if (SelectedDebugProfile != null && SelectedDebugProfile is IWritablePersistOption writablePersist)
                {
                    writablePersist.DoNotPersist = false;
                }
            }
        }

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

        public LaunchType SelectedLaunchType
        {
            get
            {
                return _selectedLaunchType;
            }
            set
            {
                ILaunchSettingsUIProvider oldActiveProvider = ActiveProvider;
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
                            ExecutablePath ??= string.Empty;
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
                        OnPropertyChanged(nameof(SupportsRemoteDebug));
                        OnPropertyChanged(nameof(SupportsLaunchUrl));
                        OnPropertyChanged(nameof(SupportsEnvironmentVariables));
                        OnPropertyChanged(nameof(SupportNativeDebugging));
                        OnPropertyChanged(nameof(SupportSqlDebugging));
                        OnPropertyChanged(nameof(SupportJSWebView2Debugging));
                        OnPropertyChanged(nameof(ActiveProviderUserControl));
                        OnPropertyChanged(nameof(DoesNotHaveErrors));
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

        public bool RemoteDebugEnabled
        {
            get { return GetOtherProperty(LaunchProfileExtensions.RemoteDebugEnabledProperty, defaultValue: false); }
            set
            {
                if (TrySetOtherProperty(LaunchProfileExtensions.RemoteDebugEnabledProperty, value, defaultValue: false))
                {
                    OnPropertyChanged(nameof(RemoteDebugEnabled));
                    OnErrorsChanged(nameof(RemoteDebugMachine)); // this property effects another's validation
                }
            }
        }

        public string RemoteDebugMachine
        {
            get { return GetOtherProperty(LaunchProfileExtensions.RemoteDebugMachineProperty, ""); }
            set
            {
                if (TrySetOtherProperty(LaunchProfileExtensions.RemoteDebugMachineProperty, value, defaultValue: null))
                {
                    OnPropertyChanged(nameof(RemoteDebugMachine));
                    OnErrorsChanged(nameof(RemoteDebugMachine));
                }
            }
        }

        public IEnumerable<IRemoteAuthenticationProvider> RemoteAuthenticationProviders => RemoteDebuggerAuthenticationService.GetRemoteAuthenticationModes();

        public IRemoteAuthenticationProvider RemoteAuthenticationProvider
        {
            get
            {
                string remoteAuthenticationMode = GetOtherProperty(LaunchProfileExtensions.RemoteAuthenticationModeProperty, "");
                return RemoteDebuggerAuthenticationService.FindProviderForAuthenticationMode(remoteAuthenticationMode);
            }
            set
            {
                if (TrySetOtherProperty(LaunchProfileExtensions.RemoteAuthenticationModeProperty, value.Name, ""))
                {
                    OnPropertyChanged(nameof(RemoteAuthenticationProvider));
                }
            }
        }

        public bool HasAuthenticationProviders => RemoteAuthenticationProviders.Any();

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

        public bool NativeCodeDebugging
        {
            get { return GetOtherProperty(LaunchProfileExtensions.NativeDebuggingProperty, false); }
            set
            {
                if (TrySetOtherProperty(LaunchProfileExtensions.NativeDebuggingProperty, value, defaultValue: false))
                {
                    OnPropertyChanged(nameof(NativeCodeDebugging));
                }
            }
        }

        public bool SqlDebugging
        {
            get { return GetOtherProperty(LaunchProfileExtensions.SqlDebuggingProperty, false); }
            set
            {
                if (TrySetOtherProperty(LaunchProfileExtensions.SqlDebuggingProperty, value, defaultValue: false))
                {
                    OnPropertyChanged(nameof(SqlDebugging));
                }
            }
        }

        public bool JSWebView2Debugging
        {
            get { return GetOtherProperty(LaunchProfileExtensions.JSWebView2DebuggingProperty, false); }
            set
            {
                if (TrySetOtherProperty(LaunchProfileExtensions.JSWebView2DebuggingProperty, value, defaultValue: false))
                {
                    // If WebView2 debugging is selected, we will disable the checkboxes for Native and SQL debugging.
                    // At the same time, also set their values to false, since we would ONLY be launching the JS Debugger
                    if (value)
                    {
                        SqlDebugging = false;
                        NativeCodeDebugging = false;
                    }                 
                    OnPropertyChanged(nameof(JSWebView2Debugging));
                }
            }
        }

        private T GetOtherProperty<T>(string propertyName, T defaultValue)
        {
            if (!IsProfileSelected)
            {
                return defaultValue;
            }

            if (SelectedDebugProfile.OtherSettings.TryGetValue(propertyName, out object value) &&
                value is T b)
            {
                return b;
            }
            else if (value is string s &&
                TypeDescriptor.GetConverter(typeof(T)) is TypeConverter converter &&
                converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    if (converter.ConvertFromString(s) is T o)
                    {
                        return o;
                    }
                }
                catch (Exception)
                {
                    // ignore bad data in the json file and just let them have the default value
                }
            }

            return defaultValue;
        }

        private bool TrySetOtherProperty<T>(string propertyName, T value, T defaultValue)
        {
            if (!SelectedDebugProfile.OtherSettings.TryGetValue(propertyName, out object current))
            {
                current = defaultValue;
            }

            if (current is not T currentTyped || !Equals(currentTyped, value))
            {
                SelectedDebugProfile.OtherSettings[propertyName] = value;
                return true;
            }

            return false;
        }

        public bool SupportNativeDebugging       => ActiveProviderSupportsProperty(UIProfilePropertyName.NativeDebugging);
        public bool SupportSqlDebugging          => ActiveProviderSupportsProperty(UIProfilePropertyName.SqlDebugging);
        public bool SupportJSWebView2Debugging   => ActiveProviderSupportsProperty(UIProfilePropertyName.JSWebView2Debugging);
        public bool SupportsExecutable           => ActiveProviderSupportsProperty(UIProfilePropertyName.Executable);
        public bool SupportsArguments            => ActiveProviderSupportsProperty(UIProfilePropertyName.Arguments);
        public bool SupportsWorkingDirectory     => ActiveProviderSupportsProperty(UIProfilePropertyName.WorkingDirectory);
        public bool SupportsRemoteDebug          => ActiveProviderSupportsProperty(UIProfilePropertyName.RemoteDebug);
        public bool SupportsLaunchUrl            => ActiveProviderSupportsProperty(UIProfilePropertyName.LaunchUrl);
        public bool SupportsEnvironmentVariables => ActiveProviderSupportsProperty(UIProfilePropertyName.EnvironmentVariables);

        public bool IsProfileSelected => SelectedDebugProfile != null;

        public ObservableCollection<IWritableLaunchProfile> LaunchProfiles { get; } = new ObservableCollection<IWritableLaunchProfile>();

        public bool HasProfilesOrNotInitialized
        {
            get
            {
                return !_debugTargetsCoreInitialized || HasProfiles;
            }
        }

        public bool HasProfiles
        {
            get
            {
                return CurrentLaunchSettings?.Profiles.Count > 0;
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
                    IWritableLaunchProfile oldProfile = CurrentLaunchSettings.ActiveProfile;
                    CurrentLaunchSettings.ActiveProfile = value;
                    NotifySelectedChanged(oldProfile);
                }
            }
        }

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
                        RemoveEnvironmentVariablesRow = EnvironmentVariablesValid || EnvironmentVariables[_environmentVariablesRowSelectedIndex].HasValidationError;
                    }

                    OnPropertyChanged(nameof(EnvironmentVariablesRowSelectedIndex), suppressInvalidation: true);
                }
            }
        }

        public bool EnvironmentVariablesValid
        {
            get
            {
                return EnvironmentVariables == null || _environmentVariablesValid;
            }
            set
            {
                if (_environmentVariablesValid != value)
                {
                    _environmentVariablesValid = value;
                    if (value)
                    {
                        ClearEnvironmentVariablesGridError?.Invoke(this, EventArgs.Empty);
                    }
                    OnPropertyChanged(nameof(EnvironmentVariablesValid), suppressInvalidation: true);
                }
            }
        }

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
        /// Provides binding to the current UI Provider user control.
        /// </summary>
        public UserControl ActiveProviderUserControl => ActiveProvider?.CustomUI;

        /// <summary>
        /// Returns the active UI provider for the current selected launchType or mull if no selection or the selected item
        /// does not have a provider installed
        /// </summary>
        private ILaunchSettingsUIProvider ActiveProvider
        {
            get
            {
                if (SelectedLaunchType == null)
                {
                    return null;
                }

                Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView> activeProvider = _uiProviders.FirstOrDefault((p) => string.Equals(p.Value.CommandName, SelectedLaunchType.CommandName, StringComparisons.LaunchProfileCommandNames));
                return activeProvider?.Value;
            }
        }

        public ICommand AddEnvironmentVariableRowCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _addEnvironmentVariableRowCommand, () =>
                    new DelegateCommand(state =>
                    {
                        var newRow = new NameValuePair(PropertyPageResources.EnvVariableNameWatermark, PropertyPageResources.EnvVariableValueWatermark, EnvironmentVariables);
                        EnvironmentVariables.Add(newRow);
                        EnvironmentVariablesRowSelectedIndex = EnvironmentVariables.Count - 1;
                        //Raise event to focus on 
                        FocusEnvironmentVariablesGridRow?.Invoke(this, EventArgs.Empty);
                    }));
            }
        }

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

        public ICommand BrowseDirectoryCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _browseDirectoryCommand, () =>
                    new DelegateCommand(state =>
                    {
                        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
                        string folder = WorkingDirectory;
                        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                        {
                            dialog.SelectedPath = folder;
                        }
                        DialogResult result = dialog.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            WorkingDirectory = dialog.SelectedPath;
                        }
                    }));
            }
        }

        public ICommand FindRemoteMachineCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _findRemoteMachineCommand, () =>
                new DelegateCommand(state =>
                {
                    string remoteDebugMachine = RemoteDebugMachine;
                    IRemoteAuthenticationProvider remoteAuthenticationProvider = RemoteAuthenticationProvider;
                    if (RemoteDebuggerAuthenticationService.ShowRemoteDiscoveryDialog(ref remoteDebugMachine, ref remoteAuthenticationProvider))
                    {
                        RemoteDebugMachine = remoteDebugMachine;
                        RemoteAuthenticationProvider = remoteAuthenticationProvider;
                    }
                }));
            }
        }

        public ICommand BrowseExecutableCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _browseExecutableCommand, () =>
                    new DelegateCommand(state =>
                    {
                        using var dialog = new System.Windows.Forms.OpenFileDialog();
                        string file = ExecutablePath;
                        if (!string.IsNullOrEmpty(file) && (file.IndexOfAny(Path.GetInvalidPathChars()) == -1) && Path.IsPathRooted(file))
                        {
                            dialog.InitialDirectory = Path.GetDirectoryName(file);
                            dialog.FileName = file;
                        }
                        dialog.Multiselect = false;
                        dialog.Filter = _executableFilter;
                        DialogResult result = dialog.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            ExecutablePath = dialog.FileName;
                        }
                    }));
            }
        }

        public bool NewProfileEnabled => LaunchProfiles != null;

        public ICommand NewProfileCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _newProfileCommand, () =>
                    new DelegateCommand(state =>
                    {
                        var dialog = new GetProfileNameDialog(Project.Services.ExportProvider.GetExportedValue<SVsServiceProvider>(),
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

        public bool DeleteProfileEnabled => IsProfileSelected;

        public ICommand DeleteProfileCommand
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _deleteProfileCommand, () =>
                    new DelegateCommand(state =>
                    {
                        IWritableLaunchProfile profileToRemove = SelectedDebugProfile;
                        SelectedDebugProfile = null;

                        CurrentLaunchSettings.Profiles.Remove(profileToRemove);
                        LaunchProfiles.Remove(profileToRemove);

                        SelectedDebugProfile = LaunchProfiles.Count > 0 ? LaunchProfiles[0] : null;
                        NotifyProfileCollectionChanged();
                    }));
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

                if (context is INotifyDataErrorInfo notifyDataErrorInfo)
                {
                    notifyDataErrorInfo.ErrorsChanged -= OnCustomUIErrorsChanged;
                }
            }

            // Now hook into the current providers notifications. We do that after having set the profile on the provider
            // so that we don't get notifications while the control is initializing. Note that this is likely the first time the 
            // custom control is asked for and we want to call it and have it created prior to setting the active profile
            UserControl customControl = ActiveProvider?.CustomUI;
            if (customControl != null)
            {
                ActiveProvider.ProfileSelected(CurrentLaunchSettings);

                context = customControl.DataContext as INotifyPropertyChanged;
                if (context != null)
                {
                    context.PropertyChanged += OnCustomUIStateChanged;
                }

                if (context is INotifyDataErrorInfo notifyDataErrorInfo)
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

        /// <summary>
        /// Helper returns true if there is an active provider and it supports the specified property
        /// </summary>
        private bool ActiveProviderSupportsProperty(string propertyName)
        {
            return ActiveProvider?.ShouldEnableProperty(propertyName) ?? false;
        }

        /// <summary>
        /// Helper called when a profile is added (new profile command), or a profile is deleted (delete profile command)
        /// </summary>
        private void NotifyProfileCollectionChanged()
        {
            OnPropertyChanged(nameof(HasProfiles));
            OnPropertyChanged(nameof(HasProfilesOrNotInitialized));
            OnPropertyChanged(nameof(NewProfileEnabled));
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
                OnPropertyChanged(nameof(NativeCodeDebugging));
                OnPropertyChanged(nameof(SqlDebugging));
                OnPropertyChanged(nameof(JSWebView2Debugging));
                OnPropertyChanged(nameof(WorkingDirectory));
                OnPropertyChanged(nameof(RemoteDebugEnabled));
                OnPropertyChanged(nameof(RemoteDebugMachine));
                OnPropertyChanged(nameof(RemoteAuthenticationProvider));

                UpdateLaunchTypes();

                ActiveProvider?.ProfileSelected(CurrentLaunchSettings);

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

        private static void UpdateActiveProfile()
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
        public virtual async Task SaveLaunchSettingsAsync()
        {
            ILaunchSettingsProvider provider = GetDebugProfileProvider();
            if (EnvironmentVariables?.Count > 0 && SelectedDebugProfile != null)
            {
                SelectedDebugProfile.EnvironmentVariables.Clear();
                foreach (NameValuePair kvp in EnvironmentVariables)
                {
                    SelectedDebugProfile.EnvironmentVariables.Add(kvp.Name, kvp.Value);
                }
            }
            else if (SelectedDebugProfile != null)
            {
                SelectedDebugProfile.EnvironmentVariables.Clear();
            }

            await provider.UpdateAndSaveSettingsAsync(CurrentLaunchSettings.ToLaunchSettings());
        }

        private void SetEnvironmentGrid(IWritableLaunchProfile oldProfile)
        {
            if (EnvironmentVariables != null && oldProfile != null)
            {
                if (_environmentVariables.Count > 0)
                {
                    oldProfile.EnvironmentVariables.Clear();
                    foreach (NameValuePair kvp in EnvironmentVariables)
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
            var args = e as ValidationStatusChangedEventArgs;
            EnvironmentVariablesValid = args.ValidationStatus;
        }

        /// <summary>
        /// Called whenever the debug targets change. Note that after a save this function will be
        /// called. It looks for changes and applies them to the UI as needed. Switching profiles
        /// will also cause this to change as the active profile is stored in the profiles snapshot.
        /// </summary>
        internal virtual void InitializeDebugTargetsCore(ILaunchSettings profiles)
        {
            IWritableLaunchSettings newSettings = profiles.ToWritableLaunchSettings();

            // Since this get's reentered if the user saves or the user switches active profiles.
            if (CurrentLaunchSettings?.SettingsDiffer(newSettings) == false)
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
                foreach (IWritableLaunchProfile profile in CurrentLaunchSettings.Profiles)
                {
                    LaunchProfiles.Add(profile);
                }

                // When loading new profiles we need to clear the launch type. This is so the external changes cause the current 
                // active provider to be refreshed
                _selectedLaunchType = null;
                NotifyProfileCollectionChanged();

                // If we have a selection, we want to leave it as is
                if (curProfileName == null || newSettings.Profiles.Find(p => LaunchProfile.IsSameProfileName(p.Name, curProfileName)) == null)
                {
                    // Note that we have to be careful since the collection can be empty. 
                    if (profiles.ActiveProfile != null && !string.IsNullOrEmpty(profiles.ActiveProfile.Name))
                    {
                        SelectedDebugProfile = LaunchProfiles.Single(p => LaunchProfile.IsSameProfileName(p.Name, profiles.ActiveProfile.Name));
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
                    SelectedDebugProfile = LaunchProfiles.Single(p => LaunchProfile.IsSameProfileName(p.Name, curProfileName));
                }
            }
            finally
            {
                PopIgnoreEvents();
                _firstSnapshotCompleteSource?.TrySetResult();
                _debugTargetsCoreInitialized = true;
            }
        }

        /// <summary>
        /// The initialization entry point for the page It also hooks into debug provider so that it can update when the profile changes
        /// </summary>
        protected void InitializePropertyPage()
        {
            if (_debugProfileProviderLink == null)
            {
                ILaunchSettingsProvider profileProvider = GetDebugProfileProvider();
                _debugProfileProviderLink = profileProvider.SourceBlock.LinkToAsyncAction(
                    OnLaunchSettingsChangedAsync,
                    Project);

                InitializeUIProviders();
            }
        }

        private async Task OnLaunchSettingsChangedAsync(ILaunchSettings profiles)
        {
            if (_firstSnapshotCompleteSource == null)
            {
                await ProjectThreadingService.SwitchToUIThread();
            }

            InitializeDebugTargetsCore(profiles);
        }

        /// <summary>
        /// initializes the collection of UI providers.
        /// </summary>
        private void InitializeUIProviders()
        {
            // We need to get the set of UI providers, if any.
            _uiProviders = new OrderPrecedenceImportCollection<ILaunchSettingsUIProvider>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, Project);
            IEnumerable<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>> uiProviders = GetUIProviders();
            foreach (Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView> uiProvider in uiProviders)
            {
                _uiProviders.Add(uiProvider);
            }
        }

        /// <summary>
        /// Gets the UI providers
        /// </summary>
        protected virtual IEnumerable<Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>> GetUIProviders()
        {
            return Project.Services.ExportProvider.GetExports<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView>();
        }

        public override Task InitializeAsync()
        {
            // Initialize the page
            InitializePropertyPage();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when then the user saves the form.
        /// </summary>
        public override async Task<int> SaveAsync()
        {
            if (HasErrors)
            {
                throw new Exception(PropertyPageResources.ErrorsMustBeCorrectedPriorToSaving);
            }

            await SaveLaunchSettingsAsync();

            return HResult.OK;
        }

        internal void CreateProfile(string name, string commandName)
        {
            var profile = new WritableLaunchProfile { Name = name, CommandName = commandName };
            CurrentLaunchSettings.Profiles.Add(profile);
            LaunchProfiles.Add(profile);

            NotifyProfileCollectionChanged();

            // Fire a property changed so we can get the page to be dirty when we add a new profile
            OnPropertyChanged("_NewProfile");
            SelectedDebugProfile = profile;
        }

        internal bool IsNewProfileNameValid(string name)
        {
            return !LaunchProfiles.Any(
                profile => LaunchProfile.IsSameProfileName(profile.Name, name));
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
        private void UpdateLaunchTypes()
        {
            // Populate the set of unique launch types from the list of providers since there can be duplicates with different priorities. However,
            // the command name will be the same so we can grab the first one for the purposes of populating the list
            if (_providerLaunchTypes == null)
            {
                _providerLaunchTypes = new List<LaunchType>();
                foreach (Lazy<ILaunchSettingsUIProvider, IOrderPrecedenceMetadataView> provider in _uiProviders)
                {
                    if (_providerLaunchTypes.Find(launchType => launchType.CommandName.Equals(provider.Value.CommandName)) == null)
                    {
                        _providerLaunchTypes.Add(new LaunchType(provider.Value.CommandName, provider.Value.FriendlyName));
                    }
                }
            }

            IWritableLaunchProfile selectedProfile = SelectedDebugProfile;
            LaunchType selectedLaunchType = null;

            _launchTypes = new List<LaunchType>();
            if (selectedProfile != null)
            {
                _launchTypes.AddRange(_providerLaunchTypes);

                selectedLaunchType = _launchTypes.Find(launchType => string.Equals(launchType.CommandName, selectedProfile.CommandName));
                if (selectedLaunchType == null)
                {
                    selectedLaunchType = new LaunchType(selectedProfile.CommandName, selectedProfile.CommandName);
                    _launchTypes.Insert(0, selectedLaunchType);
                }
            }

            // Need to notify the list has changed prior to changing the selected one
            OnPropertyChanged(nameof(LaunchTypes));

            SelectedLaunchType = selectedLaunchType;
        }

        private void EnvironmentVariables_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        protected virtual ILaunchSettingsProvider GetDebugProfileProvider()
        {
            if (_launchSettingsProvider == null)
            {
                _launchSettingsProvider = Project.Services.ExportProvider.GetExportedValue<ILaunchSettingsProvider>();
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

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(DoesNotHaveErrors));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (ActiveProvider?.CustomUI?.DataContext is INotifyDataErrorInfo notifyDataError)
            {
                foreach (object error in notifyDataError.GetErrors(propertyName))
                {
                    yield return error;
                }
            }

            if (propertyName == nameof(RemoteDebugMachine) && RemoteDebugEnabled)
            {
                if (string.IsNullOrWhiteSpace(RemoteDebugMachine))
                {
                    yield return PropertyPageResources.RemoteHostNameRequired;
                }
                else if (Uri.CheckHostName(RemoteDebugMachine) == UriHostNameType.Unknown)
                {
                    yield return PropertyPageResources.InvalidHostName;
                }
            }
        }

        /// <summary>
        /// We are considered in error if we have errors, or the currently active control has errors
        /// </summary>
        public bool HasErrors
        {
            get
            {
                bool hasRemoteDebugMachineError = RemoteDebugEnabled && Uri.CheckHostName(RemoteDebugMachine) == UriHostNameType.Unknown;

                return hasRemoteDebugMachineError ||
                    (ActiveProvider?.CustomUI?.DataContext is INotifyDataErrorInfo notifyDataError && notifyDataError.HasErrors);
            }
        }

        public bool DoesNotHaveErrors => !HasErrors;
    }
}
