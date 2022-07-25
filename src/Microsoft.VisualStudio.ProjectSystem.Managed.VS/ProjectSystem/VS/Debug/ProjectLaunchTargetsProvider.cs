// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Debugger.UI.Interfaces.HotReload;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Provides QueryDebugTargetsAsync() support for running the project output or any random executable. It is not an exported
    /// CPS debugger but hooks into the launch profiles extensibility point. The order of this provider is
    /// near the bottom to ensure other providers get chance to handle it first
    /// </summary>
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [Order(Order.Default)] // The higher the number the higher priority and we want this one last
    internal class ProjectLaunchTargetsProvider :
        IDebugProfileLaunchTargetsProvider,
        IDebugProfileLaunchTargetsProvider2,
        IDebugProfileLaunchTargetsProvider3,
        IDebugProfileLaunchTargetsProvider4
    {
        private static readonly char[] s_escapedChars = new[] { '^', '<', '>', '&' };
        private readonly ConfiguredProject _project;
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVsServices;
        private readonly IDebugTokenReplacer _tokenReplacer;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentHelper _environment;
        private readonly IActiveDebugFrameworkServices _activeDebugFramework;
        private readonly IProjectThreadingService _threadingService;
        private readonly IVsUIService<IVsDebugger10> _debugger;
        private readonly IRemoteDebuggerAuthenticationService _remoteDebuggerAuthenticationService;
        private readonly Lazy<IProjectHotReloadSessionManager> _hotReloadSessionManager;
        private readonly Lazy<IHotReloadOptionService> _debuggerSettings;
        private readonly OutputTypeChecker _outputTypeChecker;

        [ImportingConstructor]
        public ProjectLaunchTargetsProvider(
            IUnconfiguredProjectVsServices unconfiguredProjectVsServices,
            ConfiguredProject project,
            IDebugTokenReplacer tokenReplacer,
            IFileSystem fileSystem,
            IEnvironmentHelper environment,
            IActiveDebugFrameworkServices activeDebugFramework,
            ProjectProperties properties,
            IProjectThreadingService threadingService,
            IVsUIService<SVsShellDebugger, IVsDebugger10> debugger,
            IRemoteDebuggerAuthenticationService remoteDebuggerAuthenticationService,
            Lazy<IProjectHotReloadSessionManager> hotReloadSessionManager,
            Lazy<IHotReloadOptionService> debuggerSettings)
        {
            _project = project;
            _unconfiguredProjectVsServices = unconfiguredProjectVsServices;
            _tokenReplacer = tokenReplacer;
            _fileSystem = fileSystem;
            _environment = environment;
            _activeDebugFramework = activeDebugFramework;
            _threadingService = threadingService;
            _debugger = debugger;
            _remoteDebuggerAuthenticationService = remoteDebuggerAuthenticationService;
            _hotReloadSessionManager = hotReloadSessionManager;
            _debuggerSettings = debuggerSettings;

            _outputTypeChecker = new OutputTypeChecker(properties);
        }

        private Task<ConfiguredProject?> GetConfiguredProjectForDebugAsync() =>
            _activeDebugFramework.GetConfiguredProjectForActiveFrameworkAsync();

        /// <summary>
        /// This provider handles running the Project and empty commandName (this generally just runs the executable)
        /// </summary>
        public bool SupportsProfile(ILaunchProfile profile) =>
            string.IsNullOrWhiteSpace(profile.CommandName) || IsRunProjectCommand(profile) || IsRunExecutableCommand(profile);

        /// <summary>
        /// Called just prior to launch to do additional work (put up ui, do special configuration etc).
        /// </summary>
        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            throw new InvalidOperationException($"Wrong overload of {nameof(OnBeforeLaunchAsync)} called.");
        }

        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IReadOnlyList<IDebugLaunchSettings> debugLaunchSettings)
            => Task.CompletedTask;

        /// <summary>
        /// Called just after the launch to do additional work (put up ui, do special configuration etc).
        /// </summary>
        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            throw new InvalidOperationException($"Wrong overload of {nameof(OnAfterLaunchAsync)} called.");
        }

        public async Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IReadOnlyList<VsDebugTargetProcessInfo> processInfos)
        {
            await TaskScheduler.Default;

            bool runningUnderDebugger = (launchOptions & DebugLaunchOptions.NoDebug) != DebugLaunchOptions.NoDebug;

            await _hotReloadSessionManager.Value.ActivateSessionAsync((int)processInfos[0].dwProcessId, runningUnderDebugger, Path.GetFileNameWithoutExtension(_project.UnconfiguredProject.FullPath));
        }

        public async Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            if (IsRunProjectCommand(profile))
            {
                // If the profile uses the "Project" command, check that the project specifies
                // something we can run.

                ConfiguredProject? configuredProject = await GetConfiguredProjectForDebugAsync();
                Assumes.NotNull(configuredProject);
                Assumes.Present(configuredProject.Services.ProjectPropertiesProvider);

                IProjectProperties properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

                string? runCommand = await GetTargetCommandAsync(properties, validateSettings: true);
                if (string.IsNullOrWhiteSpace(runCommand))
                {
                    return false;
                }
            }

            // Otherwise, the profile must be using the "Executable" command in which case it
            // can always be a start-up project.
            return true;
        }

        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsForDebugLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile) =>
            await QueryDebugTargetsAsync(launchOptions, activeProfile, validateSettings: true) ?? throw new Exception(VSResources.ProjectNotRunnableDirectly);

        /// <summary>
        /// This is called on F5/Ctrl-F5 to return the list of debug targets. What we return depends on the type
        /// of project.
        /// </summary>
        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile) =>
            await QueryDebugTargetsAsync(launchOptions, activeProfile, validateSettings: false) ?? throw new Exception(VSResources.ProjectNotRunnableDirectly);

        /// <summary>
        /// Returns <see langword="null"/> if the debug launch settings are <see langword="null"/>. Otherwise, the list of debug launch settings.
        /// </summary>
        private async Task<IReadOnlyList<IDebugLaunchSettings>?> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile, bool validateSettings)
        {
            // Resolve the tokens in the profile
            ILaunchProfile resolvedProfile = await _tokenReplacer.ReplaceTokensInProfileAsync(activeProfile);

            DebugLaunchSettings? consoleTarget = await GetConsoleTargetForProfileAsync(resolvedProfile, launchOptions, validateSettings);
            return consoleTarget is null ? null : new[] { consoleTarget };
        }

        /// <summary>
        /// Does some basic validation of the settings. If we don't, the error messages are terrible.
        /// </summary>
        public void ValidateSettings([NotNull] string? executable, string workingDir, string? profileName)
        {
            if (Strings.IsNullOrEmpty(executable))
            {
                throw new ProjectLaunchSettingValidationException(string.Format(VSResources.NoDebugExecutableSpecified, profileName));
            }

            if (executable.IndexOf(Path.DirectorySeparatorChar) != -1 && !_fileSystem.FileExists(executable))
            {
                throw new ProjectLaunchSettingValidationException(string.Format(VSResources.DebugExecutableNotFound, executable, profileName));
            }

            if (!string.IsNullOrEmpty(workingDir) && !_fileSystem.DirectoryExists(workingDir))
            {
                throw new ProjectLaunchSettingValidationException(string.Format(VSResources.WorkingDirecotryInvalid, workingDir, profileName));
            }
        }

        private sealed class ProjectLaunchSettingValidationException : Exception
        {
            public ProjectLaunchSettingValidationException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Helper returns cmd.exe as the launcher for Ctrl-F5 (useCmdShell == true), otherwise just the exe and args passed in.
        /// </summary>
        public static void GetExeAndArguments(bool useCmdShell, string? debugExe, string? debugArgs, out string? finalExePath, out string? finalArguments)
        {
            if (useCmdShell)
            {
                // Escape the characters ^<>& so that they are passed to the application rather than interpreted by cmd.exe.
                string? escapedArgs = EscapeString(debugArgs, s_escapedChars);
                finalArguments = $"/c \"\"{debugExe}\" {escapedArgs} & pause\"";
                finalExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            }
            else
            {
                finalArguments = debugArgs;
                finalExePath = debugExe;
            }
        }

        private static bool IsRunExecutableCommand(ILaunchProfile profile) =>
            string.Equals(profile.CommandName, LaunchSettingsProvider.RunExecutableCommandName, StringComparisons.LaunchProfileCommandNames);

        private static bool IsRunProjectCommand(ILaunchProfile profile) =>
            string.Equals(profile.CommandName, LaunchSettingsProvider.RunProjectCommandName, StringComparisons.LaunchProfileCommandNames);

        private async Task<bool> IsIntegratedConsoleEnabledAsync()
        {
            if (!_project.Capabilities.Contains(ProjectCapabilities.IntegratedConsoleDebugging))
                return false;

            await _threadingService.SwitchToUIThread();

            _debugger.Value.IsIntegratedConsoleEnabled(out bool enabled);

            return enabled;
        }

        /// <summary>
        /// This is called on F5 to return the list of debug targets. What we return depends on the type
        /// of project.
        /// </summary>
        /// <returns><see langword="null"/> if the runnable project information is <see langword="null"/>. Otherwise, the debug launch settings.</returns>
        internal async Task<DebugLaunchSettings?> GetConsoleTargetForProfileAsync(ILaunchProfile resolvedProfile, DebugLaunchOptions launchOptions, bool validateSettings)
        {
            var settings = new DebugLaunchSettings(launchOptions);

            string? executable, arguments;

            string projectFolder = Path.GetDirectoryName(_project.UnconfiguredProject.FullPath) ?? string.Empty;
            ConfiguredProject? configuredProject = await GetConfiguredProjectForDebugAsync();

            Assumes.NotNull(configuredProject);

            // If no working directory specified in the profile, we default to output directory. If for some reason the output directory
            // is not specified, fall back to the project folder.
            string defaultWorkingDir = await GetOutputDirectoryAsync(configuredProject);
            if (string.IsNullOrEmpty(defaultWorkingDir))
            {
                defaultWorkingDir = projectFolder;
            }
            else
            {
                if (!Path.IsPathRooted(defaultWorkingDir))
                {
                    defaultWorkingDir = _fileSystem.GetFullPath(Path.Combine(projectFolder, defaultWorkingDir));
                }

                // If the directory at OutDir doesn't exist, fall back to the project folder
                if (!_fileSystem.DirectoryExists(defaultWorkingDir))
                {
                    defaultWorkingDir = projectFolder;
                }
            }

            string? commandLineArgs = resolvedProfile.CommandLineArgs is null
                ? null
                : Regex.Replace(resolvedProfile.CommandLineArgs, "[\r\n]+", " ");

            // Is this profile just running the project? If so we ignore the exe
            if (IsRunProjectCommand(resolvedProfile))
            {
                // Get the executable to run, the arguments and the default working directory
                (string Command, string Arguments, string WorkingDirectory)? runnableProjectInfo = await GetRunnableProjectInformationAsync(configuredProject, validateSettings);
                if (runnableProjectInfo == null)
                {
                    return null;
                }
                string workingDirectory;
                (executable, arguments, workingDirectory) = runnableProjectInfo.Value;

                if (!string.IsNullOrWhiteSpace(workingDirectory))
                {
                    defaultWorkingDir = workingDirectory;
                }

                if (!string.IsNullOrWhiteSpace(commandLineArgs))
                {
                    arguments = arguments + " " + commandLineArgs;
                }
            }
            else
            {
                executable = resolvedProfile.ExecutablePath;
                arguments = commandLineArgs;
            }

            string workingDir;
            if (Strings.IsNullOrWhiteSpace(resolvedProfile.WorkingDirectory))
            {
                workingDir = defaultWorkingDir;
            }
            else
            {
                // If the working directory is not rooted we assume it is relative to the project directory
                workingDir = _fileSystem.GetFullPath(Path.Combine(projectFolder, resolvedProfile.WorkingDirectory.Replace("/", "\\")));
            }

            // IF the executable is not rooted, we want to make is relative to the workingDir unless is doesn't contain
            // any path elements. In that case we are going to assume it is in the current directory of the VS process, or on
            // the environment path. If we can't find it, we just launch it as before.
            if (!Strings.IsNullOrWhiteSpace(executable))
            {
                executable = executable.Replace("/", "\\");
                if (Path.GetPathRoot(executable) == "\\")
                {
                    // Root of current drive
                    executable = _fileSystem.GetFullPath(executable);
                }
                else if (!Path.IsPathRooted(executable))
                {
                    if (executable.Contains("\\"))
                    {
                        // Combine with the working directory used by the profile
                        executable = _fileSystem.GetFullPath(Path.Combine(workingDir, executable));
                    }
                    else
                    {
                        // Try to resolve against the current working directory (for compat) and failing that, the environment path.
                        string exeName = executable.EndsWith(".exe", StringComparisons.Paths) ? executable : executable + ".exe";
                        string fullPath = _fileSystem.GetFullPath(exeName);
                        if (_fileSystem.FileExists(fullPath))
                        {
                            executable = fullPath;
                        }
                        else
                        {
                            string? fullPathFromEnv = GetFullPathOfExeFromEnvironmentPath(exeName);
                            if (fullPathFromEnv is not null)
                            {
                                executable = fullPathFromEnv;
                            }
                        }
                    }
                }
            }

            if (validateSettings)
            {
                ValidateSettings(executable, workingDir, resolvedProfile.Name);
            }

            // Apply environment variables.
            foreach ((string key, string value) in resolvedProfile.EnumerateEnvironmentVariables())
            {
                settings.Environment[key] = value;
            }

            settings.LaunchOperation = DebugLaunchOperation.CreateProcess;
            settings.LaunchDebugEngineGuid = await GetDebuggingEngineAsync(configuredProject);

            if (resolvedProfile.IsNativeDebuggingEnabled())
            {
                settings.AdditionalDebugEngines.Add(DebuggerEngines.NativeOnlyEngine);
            }

            if (resolvedProfile.IsSqlDebuggingEnabled())
            {
                settings.AdditionalDebugEngines.Add(DebuggerEngines.SqlEngine);
            }

            bool useCmdShell = false;
            if (await _outputTypeChecker.IsConsoleAsync())
            {
                if (await IsIntegratedConsoleEnabledAsync())
                {
                    settings.LaunchOptions |= DebugLaunchOptions.IntegratedConsole;
                }

                useCmdShell = UseCmdShellForConsoleLaunch(resolvedProfile, settings.LaunchOptions);
            }

            GetExeAndArguments(useCmdShell, executable, arguments, out string? finalExecutable, out string? finalArguments);

            settings.Executable = finalExecutable;
            settings.Arguments = finalArguments;
            settings.CurrentDirectory = workingDir;
            settings.Project = _unconfiguredProjectVsServices.VsHierarchy;

            if (resolvedProfile.IsRemoteDebugEnabled())
            {
                settings.RemoteMachine = resolvedProfile.RemoteDebugMachine();

                string? remoteAuthenticationMode = resolvedProfile.RemoteAuthenticationMode();
                if (!Strings.IsNullOrEmpty(remoteAuthenticationMode))
                {
                    IRemoteAuthenticationProvider? remoteAuthenticationProvider = _remoteDebuggerAuthenticationService.FindProviderForAuthenticationMode(remoteAuthenticationMode);
                    if (remoteAuthenticationProvider is not null)
                    {
                        settings.PortSupplierGuid = remoteAuthenticationProvider.PortSupplierGuid;
                    }
                }
            }

            // WebView2 debugging is only supported for Project and Executable commands
            if (resolvedProfile.IsJSWebView2DebuggingEnabled() && (IsRunExecutableCommand(resolvedProfile) || IsRunProjectCommand(resolvedProfile)))
            {
                // If JS Debugger is selected, we would need to change the launch debugger to that one
                settings.LaunchDebugEngineGuid = DebuggerEngines.JavaScriptForWebView2Engine;

                // Create the launch params needed for the JS debugger
                var debuggerLaunchOptions = new JObject(
                        new JProperty("type", "pwa-msedge"),
                        new JProperty("runtimeExecutable", finalExecutable),
                        new JProperty("webRoot", workingDir), // We use the Working Directory debugging option as the WebRoot, to map the urls to files on disk
                        new JProperty("useWebView", true),
                        new JProperty("runtimeArgs", finalArguments)
                    );

                settings.Options = JsonConvert.SerializeObject(debuggerLaunchOptions);
            }

            if (await HotReloadShouldBeEnabledAsync(resolvedProfile, launchOptions)
                && await _hotReloadSessionManager.Value.TryCreatePendingSessionAsync(settings.Environment))
            {
                // Enable XAML Hot Reload
                settings.Environment["ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO"] = "1";
            }

            if (settings.Environment.Count > 0)
            {
                settings.LaunchOptions |= DebugLaunchOptions.MergeEnvironment;
            }

            return settings;
        }

        private async Task<bool> HotReloadShouldBeEnabledAsync(ILaunchProfile resolvedProfile, DebugLaunchOptions launchOptions)
        {
            bool hotReloadEnabledAtProjectLevel = IsRunProjectCommand(resolvedProfile)
                && resolvedProfile.IsHotReloadEnabled()
                && !resolvedProfile.IsRemoteDebugEnabled()
                && (launchOptions & DebugLaunchOptions.Profiling) != DebugLaunchOptions.Profiling;

            if (hotReloadEnabledAtProjectLevel)
            {
                bool debugging = (launchOptions & DebugLaunchOptions.NoDebug) != DebugLaunchOptions.NoDebug;
                bool hotReloadEnabledGlobally = await _debuggerSettings.Value.IsHotReloadEnabledAsync(debugging, default);

                return hotReloadEnabledGlobally;
            }

            return false;
        }

        private static bool UseCmdShellForConsoleLaunch(ILaunchProfile profile, DebugLaunchOptions options)
        {
            if (!IsRunProjectCommand(profile))
                return false;

            if ((options & DebugLaunchOptions.IntegratedConsole) == DebugLaunchOptions.IntegratedConsole)
                return false;

            if ((options & DebugLaunchOptions.Profiling) == DebugLaunchOptions.Profiling)
                return false;

            return (options & DebugLaunchOptions.NoDebug) == DebugLaunchOptions.NoDebug;
        }

        /// <summary>
        /// Queries properties from the project to get information on how to run the application. The returned Tuple contains:
        /// exeToRun, arguments, workingDir
        /// </summary>
        /// <returns><see langword="null"/> if the command string is <see langword="null"/>. Otherwise, the tuple containing the runnable project information.</returns>
        private async Task<(string Command, string Arguments, string WorkingDirectory)?> GetRunnableProjectInformationAsync(
            ConfiguredProject configuredProject,
            bool validateSettings)
        {
            Assumes.Present(configuredProject.Services.ProjectPropertiesProvider);

            IProjectProperties properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

            string? runCommand = await GetTargetCommandAsync(properties, validateSettings);
            if (string.IsNullOrWhiteSpace(runCommand))
            {
                return null;
            }

            string runWorkingDirectory = await properties.GetEvaluatedPropertyValueAsync("RunWorkingDirectory");
            // If the working directory is relative, it will be relative to the project root so make it a full path
            if (!string.IsNullOrWhiteSpace(runWorkingDirectory) && !Path.IsPathRooted(runWorkingDirectory))
            {
                runWorkingDirectory = Path.Combine(Path.GetDirectoryName(_project.UnconfiguredProject.FullPath) ?? string.Empty, runWorkingDirectory);
            }

            string runArguments = await properties.GetEvaluatedPropertyValueAsync("RunArguments");

            return (runCommand!, runArguments, runWorkingDirectory);
        }

        /// <summary>
        /// Returns <see langword="null"/> if it is not a valid debug target. Otherwise, returns the command string for debugging.
        /// </summary>
        private async Task<string?> GetTargetCommandAsync(
            IProjectProperties properties,
            bool validateSettings)
        {
            // First try "RunCommand" property
            string? runCommand = await GetRunCommandAsync(properties);

            if (Strings.IsNullOrEmpty(runCommand))
            {
                // If we're launching for debug purposes, prevent someone F5'ing a class library
                if (validateSettings && await _outputTypeChecker.IsLibraryAsync())
                {
                    return null;
                }

                // Otherwise, fall back to "TargetPath"
                runCommand = await properties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPathProperty);
            }

            return runCommand;
        }

        private async Task<string?> GetRunCommandAsync(IProjectProperties properties)
        {
            string runCommand = await properties.GetEvaluatedPropertyValueAsync("RunCommand");

            if (string.IsNullOrEmpty(runCommand))
            {
                return null;
            }

            // If dotnet.exe is used runCommand returns just "dotnet". The debugger is going to require a full path so we need to append the .exe
            // extension.
            if (!runCommand.EndsWith(".exe", StringComparisons.Paths))
            {
                runCommand += ".exe";
            }

            // If the path is just the name of an exe like dotnet.exe then we try to find it on the path
            if (runCommand.IndexOf(Path.DirectorySeparatorChar) == -1)
            {
                string? executable = GetFullPathOfExeFromEnvironmentPath(runCommand);
                if (executable is not null)
                {
                    runCommand = executable;
                }
            }

            return runCommand;
        }

        private static async Task<string> GetOutputDirectoryAsync(ConfiguredProject configuredProject)
        {
            Assumes.Present(configuredProject.Services.ProjectPropertiesProvider);

            IProjectProperties properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

            return await properties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.OutDirProperty);
        }

        private static async Task<Guid> GetDebuggingEngineAsync(ConfiguredProject configuredProject)
        {
            Assumes.Present(configuredProject.Services.ProjectPropertiesProvider);

            IProjectProperties properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            string framework = await properties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetFrameworkIdentifierProperty);

            return GetManagedDebugEngineForFramework(framework);
        }

        /// <summary>
        /// Searches the path variable for the first match of exeToSearchFor. Returns
        /// null if not found.
        /// </summary>
        private string? GetFullPathOfExeFromEnvironmentPath(string exeToSearchFor)
        {
            string? pathEnv = _environment.GetEnvironmentVariable("Path");

            if (Strings.IsNullOrEmpty(pathEnv))
            {
                return null;
            }

            foreach (string path in new LazyStringSplit(pathEnv, ';'))
            {
                // We don't want one bad path entry to derail the search
                try
                {
                    string exePath = Path.Combine(path, exeToSearchFor);
                    if (_fileSystem.FileExists(exePath))
                    {
                        return exePath;
                    }
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }

        /// <summary>
        /// Escapes the given characters in a given string, ignoring escape sequences when inside a quoted string.
        /// </summary>
        /// <param name="unescaped">The string to escape.</param>
        /// <param name="toEscape">The characters to escape in the string.</param>
        /// <returns>The escaped string.</returns>
        [return: NotNullIfNotNull("unescaped")]
        internal static string? EscapeString(string? unescaped, char[] toEscape)
        {
            if (Strings.IsNullOrWhiteSpace(unescaped))
            {
                return unescaped;
            }

            bool ShouldEscape(char c)
            {
                foreach (char escapeChar in toEscape)
                {
                    if (escapeChar == c)
                        return true;
                }
                return false;
            }

            StringState currentState = StringState.NormalCharacter;
            var finalBuilder = PooledStringBuilder.GetInstance();
            foreach (char currentChar in unescaped)
            {
                switch (currentState)
                {
                    case StringState.NormalCharacter:
                        // If we're currently not in a quoted string, then we need to escape anything in toEscape.
                        // The valid transitions are to EscapedCharacter (for a '\', such as '\"'), and QuotedString.
                        if (currentChar == '\\')
                        {
                            currentState = StringState.EscapedCharacter;
                        }
                        else if (currentChar == '"')
                        {
                            currentState = StringState.QuotedString;
                        }
                        else if (ShouldEscape(currentChar))
                        {
                            finalBuilder.Append('^');
                        }

                        break;
                    case StringState.EscapedCharacter:
                        // If a '\' was the previous character, then we blindly append to the string, escaping if necessary,
                        // and move back to NormalCharacter. This handles '\"'
                        if (ShouldEscape(currentChar))
                        {
                            finalBuilder.Append('^');
                        }

                        currentState = StringState.NormalCharacter;
                        break;
                    case StringState.QuotedString:
                        // If we're in a string, we don't escape any characters. If the current character is a '\',
                        // then we move to QuotedStringEscapedCharacter. This handles '\"'. If the current character
                        // is a '"', then we're out of the string. Otherwise, we stay in the string.
                        if (currentChar == '\\')
                        {
                            currentState = StringState.QuotedStringEscapedCharacter;
                        }
                        else if (currentChar == '"')
                        {
                            currentState = StringState.NormalCharacter;
                        }

                        break;
                    case StringState.QuotedStringEscapedCharacter:
                        // If we have one slash, then we blindly append to the string, no escaping, and move back to
                        // QuotedString. This handles escaped '"' inside strings.
                        currentState = StringState.QuotedString;
                        break;
                    default:
                        // We can't get here.
                        throw new InvalidOperationException();
                }

                finalBuilder.Append(currentChar);
            }

            return finalBuilder.ToStringAndFree();
        }

        /// <summary>
        /// Helper returns the correct debugger engine based on the targeted framework
        /// </summary>
        internal static Guid GetManagedDebugEngineForFramework(string targetFramework) =>
            // The engine depends on the framework
            IsDotNetCoreFramework(targetFramework) ?
                DebuggerEngines.ManagedCoreEngine :
                DebuggerEngines.ManagedOnlyEngine;

        /// <summary>
        /// TODO: This is a placeholder until issue https://github.com/dotnet/project-system/issues/423 is addressed.
        /// This information should come from the targets file.
        /// </summary>
        private static bool IsDotNetCoreFramework(string targetFramework)
        {
            const string NetStandardPrefix = ".NetStandard";
            const string NetCorePrefix = ".NetCore";
            return targetFramework.StartsWith(NetCorePrefix, StringComparisons.FrameworkIdentifiers) ||
                   targetFramework.StartsWith(NetStandardPrefix, StringComparisons.FrameworkIdentifiers);
        }

        private enum StringState
        {
            NormalCharacter, EscapedCharacter, QuotedString, QuotedStringEscapedCharacter
        }
    }
}
