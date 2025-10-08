// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Debugger.Contracts;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.UI.Interfaces.HotReload;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.HotReload;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug;

/// <summary>
/// Provides <see cref="IDebugProfileLaunchTargetsProvider.QueryDebugTargetsAsync(DebugLaunchOptions, ILaunchProfile)"/>
/// support for running the project output or any random executable. It is not an exported
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
    IDebugProfileLaunchTargetsProvider4,
    IDebugProfileLaunchTargetsProvider5
{
    private readonly ConfiguredProject _project;
    private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVsServices;
    private readonly IDebugTokenReplacer _tokenReplacer;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentHelper _environment;
    private readonly IActiveDebugFrameworkServices _activeDebugFramework;
    private readonly IProjectThreadingService _threadingService;
    private readonly IVsUIService<IVsDebugger10> _debugger;
    private readonly IRemoteDebuggerAuthenticationService _remoteDebuggerAuthenticationService;
    private readonly Lazy<IHotReloadOptionService> _hotReloadOptionService;
    private readonly IOutputTypeChecker _outputTypeChecker;

    [ImportingConstructor]
    public ProjectLaunchTargetsProvider(
        IUnconfiguredProjectVsServices unconfiguredProjectVsServices,
        ConfiguredProject project,
        IDebugTokenReplacer tokenReplacer,
        IFileSystem fileSystem,
        IEnvironmentHelper environment,
        IActiveDebugFrameworkServices activeDebugFramework,
        IOutputTypeChecker outputTypeChecker,
        IProjectThreadingService threadingService,
        IVsUIService<SVsShellDebugger, IVsDebugger10> debugger,
        IRemoteDebuggerAuthenticationService remoteDebuggerAuthenticationService,
        Lazy<IHotReloadOptionService> hotReloadOptionService)
    {
        _project = project;
        _unconfiguredProjectVsServices = unconfiguredProjectVsServices;
        _tokenReplacer = tokenReplacer;
        _fileSystem = fileSystem;
        _environment = environment;
        _activeDebugFramework = activeDebugFramework;
        _outputTypeChecker = outputTypeChecker;
        _threadingService = threadingService;
        _debugger = debugger;
        _remoteDebuggerAuthenticationService = remoteDebuggerAuthenticationService;
        _hotReloadOptionService = hotReloadOptionService;
    }

    // internal for testing
    internal ConfiguredProject Project => _project;

    private async ValueTask<ConfiguredProject> GetConfiguredProjectForDebugAsync()
    {
        var project = await _activeDebugFramework.GetConfiguredProjectForActiveFrameworkAsync();
        Assumes.NotNull(project);
        return project;
    }

    /// <summary>
    /// This provider handles running the Project and empty commandName (this generally just runs the executable)
    /// </summary>
    public bool SupportsProfile(ILaunchProfile profile)
    {
        return string.IsNullOrWhiteSpace(profile.CommandName) || profile.IsRunProjectCommand() || profile.IsRunExecutableCommand();
    }

    /// <summary>
    /// Called just prior to launch to do additional work (put up ui, do special configuration etc).
    /// </summary>
    Task IDebugProfileLaunchTargetsProvider.OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
    {
        throw new InvalidOperationException($"Wrong overload of {nameof(OnBeforeLaunchAsync)} called.");
    }

    public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IReadOnlyList<IDebugLaunchSettings> debugLaunchSettings)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called just after the launch to do additional work (put up ui, do special configuration etc).
    /// </summary>
    Task IDebugProfileLaunchTargetsProvider.OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
    {
        throw new InvalidOperationException($"Wrong overload of {nameof(OnAfterLaunchAsync)} called.");
    }

    public async Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile, IReadOnlyList<VsDebugTargetProcessInfo> processInfos)
    {
        var configuredProjectForDebug = await GetConfiguredProjectForDebugAsync();
        var hotReloadSessionManager = configuredProjectForDebug.GetExportedService<IProjectHotReloadSessionManager>();
        await hotReloadSessionManager.ActivateSessionAsync(null, processInfos[0]);
    }

    public async Task OnAfterLaunchAsync(
        DebugLaunchOptions launchOptions,
        ILaunchProfile profile,
        IDebugLaunchSettings debugLaunchSetting,
        IVsLaunchedProcess vsLaunchedProcess,
        VsDebugTargetProcessInfo processInfo)
    {
        var configuredProjectForDebug = await GetConfiguredProjectForDebugAsync();
        var hotReloadSessionManager = configuredProjectForDebug.GetExportedService<IProjectHotReloadSessionManager>();
        await hotReloadSessionManager.ActivateSessionAsync(vsLaunchedProcess, processInfo);
    }

    public async Task<bool> CanBeStartupProjectAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
    {
        if (profile.IsRunProjectCommand())
        {
            // If the profile uses the "Project" command, check that the project specifies
            // something we can run.

            ConfiguredProject activeConfiguredProject = await GetConfiguredProjectForDebugAsync();
            Assumes.Present(activeConfiguredProject.Services.ProjectPropertiesProvider);

            IProjectProperties properties = activeConfiguredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

            string? runCommand = await ProjectAndExecutableLaunchHandlerHelpers.GetTargetCommandAsync(properties, _environment, _fileSystem, _outputTypeChecker, validateSettings: true);

            if (string.IsNullOrWhiteSpace(runCommand))
            {
                return false;
            }
        }

        // Otherwise, the profile must be using the "Executable" command in which case it
        // can always be a start-up project.
        return true;
    }

    public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsForDebugLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile)
    {
        return await QueryDebugTargetsAsync(launchOptions, activeProfile, validateSettings: true) ?? throw new Exception(VSResources.ProjectNotRunnableDirectly);
    }

    /// <summary>
    /// This is called on F5/Ctrl-F5 to return the list of debug targets. What we return depends on the type
    /// of project.
    /// </summary>
    public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile)
    {
        return await QueryDebugTargetsAsync(launchOptions, activeProfile, validateSettings: false) ?? throw new Exception(VSResources.ProjectNotRunnableDirectly);
    }

    /// <summary>
    /// Returns the list of debug launch settings.
    /// </summary>
    /// <exception cref="Exception">The project is not runnable.</exception>
    private async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile, bool validateSettings)
    {
        // Resolve the tokens in the profile
        ILaunchProfile resolvedProfile = await _tokenReplacer.ReplaceTokensInProfileAsync(activeProfile);

        DebugLaunchSettings consoleTarget
            = await GetConsoleTargetForProfileAsync(resolvedProfile, launchOptions, validateSettings)
                ?? throw new Exception(VSResources.ProjectNotRunnableDirectly);

        return [consoleTarget];
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

    private sealed class ProjectLaunchSettingValidationException(string message) : Exception(message);

    /// <summary>
    /// Helper returns cmd.exe as the launcher for Ctrl-F5 (useCmdShell == true), otherwise just the exe and args passed in.
    /// </summary>
    public static void GetExeAndArguments(bool useCmdShell, string? debugExe, string? debugArgs, out string? finalExePath, out string? finalArguments)
    {
        if (useCmdShell)
        {
            // Any debug arguments must be escaped.
            finalArguments = debugArgs is null
                ? $"/c \"\"{debugExe}\" & pause\""
                : $"/c \"\"{debugExe}\" {CommandEscaping.EscapeString(debugArgs)} & pause\"";

            finalExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        }
        else
        {
            finalArguments = debugArgs;
            finalExePath = debugExe;
        }
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

        ConfiguredProject activeConfiguredProject = await GetConfiguredProjectForDebugAsync();

        // If no working directory specified in the profile, we default to output directory. If for some reason the output directory
        // is not specified, fall back to the project folder.
        string projectFolderFullPath = _project.UnconfiguredProject.GetProjectDirectory();
        string defaultWorkingDir = await ProjectAndExecutableLaunchHandlerHelpers.GetDefaultWorkingDirectoryAsync(activeConfiguredProject, projectFolderFullPath, _fileSystem);

        string? commandLineArgs = resolvedProfile.CommandLineArgs is null
            ? null
            : Regex.Replace(resolvedProfile.CommandLineArgs, "[\r\n]+", " ");

        // Is this profile just running the project? If so we ignore the exe
        if (resolvedProfile.IsRunProjectCommand())
        {
            // Get the executable to run, the arguments and the default working directory
            (string, string, string)? runnableProjectInfo = await ProjectAndExecutableLaunchHandlerHelpers.GetRunnableProjectInformationAsync(
                activeConfiguredProject,
                _environment,
                _fileSystem,
                _outputTypeChecker,
                validateSettings);

            if (runnableProjectInfo is null)
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
            workingDir = _fileSystem.GetFullPath(Path.Combine(projectFolderFullPath, resolvedProfile.WorkingDirectory.Replace("/", "\\")));
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
                        string? fullPathFromEnv = ProjectAndExecutableLaunchHandlerHelpers.GetFullPathOfExeFromEnvironmentPath(exeName, _environment, _fileSystem);
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
        settings.LaunchDebugEngineGuid = await GetDebuggingEngineAsync(activeConfiguredProject);

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
        if (resolvedProfile.IsJSWebView2DebuggingEnabled() && (resolvedProfile.IsRunExecutableCommand() || resolvedProfile.IsRunProjectCommand()))
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

        if (await HotReloadShouldBeEnabledAsync(resolvedProfile, launchOptions))
        {
            var hotReloadSessionManager = activeConfiguredProject.GetExportedService<IProjectHotReloadSessionManager>();
            var projectHotReloadLaunchProvider = activeConfiguredProject.GetExportedService<IProjectHotReloadLaunchProvider>();

            if (await hotReloadSessionManager.TryCreatePendingSessionAsync(
                launchProvider: projectHotReloadLaunchProvider,
                settings.Environment,
                launchOptions,
                resolvedProfile))
            {
                // Enable XAML Hot Reload
                settings.Environment["ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO"] = "1";
            }
        }

        if (settings.Environment.Count > 0)
        {
            settings.LaunchOptions |= DebugLaunchOptions.MergeEnvironment;
        }

        return settings;

        static async Task<Guid> GetDebuggingEngineAsync(ConfiguredProject configuredProject)
        {
            Assumes.Present(configuredProject.Services.ProjectPropertiesProvider);

            IProjectProperties properties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            string framework = await properties.GetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetFrameworkIdentifierProperty);

            return GetManagedDebugEngineForFramework(framework);
        }

        async Task<bool> IsIntegratedConsoleEnabledAsync()
        {
            if (!_project.Capabilities.Contains(ProjectCapabilities.IntegratedConsoleDebugging))
            {
                return false;
            }

            await _threadingService.SwitchToUIThread();

            _debugger.Value.IsIntegratedConsoleEnabled(out bool enabled);

            return enabled;
        }

        static bool UseCmdShellForConsoleLaunch(ILaunchProfile profile, DebugLaunchOptions options)
        {
            if (!profile.IsRunProjectCommand())
                return false;

            if ((options & DebugLaunchOptions.IntegratedConsole) == DebugLaunchOptions.IntegratedConsole)
                return false;

            if ((options & DebugLaunchOptions.Profiling) == DebugLaunchOptions.Profiling)
                return false;

            return (options & DebugLaunchOptions.NoDebug) == DebugLaunchOptions.NoDebug;
        }

        async Task<bool> HotReloadShouldBeEnabledAsync(ILaunchProfile resolvedProfile, DebugLaunchOptions launchOptions)
        {
            bool hotReloadEnabledAtProjectLevel = resolvedProfile.IsRunProjectCommand()
                && resolvedProfile.IsHotReloadEnabled()
                && !resolvedProfile.IsRemoteDebugEnabled()
                && (launchOptions & DebugLaunchOptions.Profiling) != DebugLaunchOptions.Profiling;

            if (hotReloadEnabledAtProjectLevel)
            {
                bool debugging = (launchOptions & DebugLaunchOptions.NoDebug) != DebugLaunchOptions.NoDebug;
                bool hotReloadEnabledGlobally = await _hotReloadOptionService.Value.IsHotReloadEnabledAsync(debugging, default);

                return hotReloadEnabledGlobally;
            }

            return false;
        }
    }

    /// <summary>
    /// Helper returns the correct debugger engine based on the targeted framework
    /// </summary>
    internal static Guid GetManagedDebugEngineForFramework(string targetFramework)
    {
        // The engine depends on the framework
        return IsDotNetCoreFramework(targetFramework)
             ? DebuggerEngines.ManagedCoreEngine
             : DebuggerEngines.ManagedOnlyEngine;

        static bool IsDotNetCoreFramework(string targetFramework)
        {
            // TODO: This is a placeholder until issue https://github.com/dotnet/project-system/issues/423 is addressed.
            // This information should come from the targets file.

            const string NetStandardPrefix = ".NetStandard";
            const string NetCorePrefix = ".NetCore";
            return targetFramework.StartsWith(NetCorePrefix, StringComparisons.FrameworkIdentifiers) ||
                   targetFramework.StartsWith(NetStandardPrefix, StringComparisons.FrameworkIdentifiers);
        }
    }
}
