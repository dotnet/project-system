// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using ExportOrder = Microsoft.VisualStudio.ProjectSystem.OrderAttribute;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Provides QueryDebugTargetsAsync() support for running the project output or any random executable. It is not an exported
    /// CPS debugger but hooks into the launch profiles extensibility point. The order of this provider is 
    /// near the bottom to ensure other providers get chance to handle it first
    /// </summary>
    [Export(typeof(IDebugProfileLaunchTargetsProvider))]
    [AppliesTo(ProjectCapability.LaunchProfiles)]
    [ExportOrder(10)] // The higher the number the higher priority and we want this one last
     internal class ConsoleDebugTargetsProvider : IDebugProfileLaunchTargetsProvider
    {

        [ImportingConstructor]
        public ConsoleDebugTargetsProvider(ConfiguredProject configuredProject, 
                                           IDebugTokenReplacer tokenReplacer, 
                                           IFileSystem fileSystem, 
                                           IEnvironmentHelper environment,
                                           ProjectProperties properties)
        {
            ConfiguredProject = configuredProject;
            TokenReplacer = tokenReplacer;
            TheFileSystem = fileSystem;
            TheEnvironment = environment;
            Properties = properties;
        }

        private ConfiguredProject ConfiguredProject { get; }
        private IDebugTokenReplacer TokenReplacer { get; }
        private IFileSystem TheFileSystem { get; }
        private IEnvironmentHelper TheEnvironment { get; }
        private ProjectProperties Properties { get; }

        /// <summary>
        /// This provider handles running the Project and empty commandName (this generally just runs the executable)
        /// </summary>
        public bool SupportsProfile(ILaunchProfile profile)
        {
            return string.IsNullOrWhiteSpace(profile.CommandName) ||
                profile.CommandName.Equals(LaunchSettingsProvider.RunProjectCommandName, StringComparison.OrdinalIgnoreCase) ||
                profile.CommandName.Equals(LaunchSettingsProvider.RunExecutableCommandName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Called just prior to launch to do additional work (put up ui, do special configuration etc).
        /// </summary>
        public Task OnBeforeLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called just after the launch to do additional work (put up ui, do special configuration etc).
        /// </summary>
        public Task OnAfterLaunchAsync(DebugLaunchOptions launchOptions, ILaunchProfile profile)
        {
            return Task.CompletedTask;
        }

        private async Task<bool> GetIsClassLibraryAsync()
        {
            // Used by default Windows debugger to figure out whether to add an extra
            // pause to end of window when CTRL+F5'ing a console application
            var configuration = await Properties.GetConfigurationGeneralPropertiesAsync()
                                                 .ConfigureAwait(false);


            IEnumValue outputType = (IEnumValue)await configuration.OutputType.GetValueAsync()
                                                                      .ConfigureAwait(false);

            return StringComparers.PropertyValues.Equals(outputType.Name, ConfigurationGeneral.OutputTypeValues.Library);
        }

        /// <summary>
        /// This is called on F5/Ctrl-F5 to return the list of debug targets.What we return depends on the type
        /// of project.  
        /// </summary>
        public async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions, ILaunchProfile activeProfile)
        {

            string projectFolder = Path.GetDirectoryName(ConfiguredProject.UnconfiguredProject.FullPath);

            List<DebugLaunchSettings> launchSettings = new List<DebugLaunchSettings>();

            var settings = new DebugLaunchSettings(launchOptions);

            // Resolve the tokens in the profile
            ILaunchProfile resolvedProfile = await TokenReplacer.ReplaceTokensInProfileAsync(activeProfile).ConfigureAwait(true);

            // We want to launch the process via the command shell when not debugging, except when this debug session is being launched for profiling.
            bool useCmdShell = (launchOptions & (DebugLaunchOptions.NoDebug | DebugLaunchOptions.Profiling)) == DebugLaunchOptions.NoDebug;
            var consoleTarget = await GetConsoleTargetForProfile(resolvedProfile, launchOptions, projectFolder, useCmdShell).ConfigureAwait(true);

            launchSettings.Add(consoleTarget);

            return launchSettings.ToArray();
        }

        /// <summary>
        /// Does some basic validation of the settings. If we don't, the error messages are terrible.
        /// </summary>
        public void ValidateSettings(string executable, string workingDir, string profileName)
        {
            if(string.IsNullOrEmpty(executable))
            {
                throw new Exception(string.Format(VSResources.NoDebugExecutableSpecified, profileName));
            }
            else if(executable.IndexOf(Path.DirectorySeparatorChar) != -1 && !TheFileSystem.FileExists(executable))
            {
                throw new Exception(string.Format(VSResources.DebugExecutableNotFound, executable, profileName));
            }
            else if(!string.IsNullOrEmpty(workingDir) && !TheFileSystem.DirectoryExists(workingDir))
            {
                throw new Exception(string.Format(VSResources.WorkingDirecotryInvalid, workingDir, profileName));
            }
        }

        /// <summary>
        /// Helper returns cmd.exe as the launcher for Ctrl-F5 (useCmdShell == true), otherwise just the exe and args passed in.
        /// </summary>
        public void GetExeAndArguments(bool useCmdShell, string debugExe, string debugArgs, out string finalExePath, out string finalArguments)
        {
            if(useCmdShell)
            {
                // Escape the characters ^<>& so that they are passed to the application rather than interpreted by cmd.exe.
                string escapedArgs = string.Empty;
                if(!string.IsNullOrWhiteSpace(debugArgs))
                {
                    escapedArgs = debugArgs.Replace("^","^^");
                    escapedArgs = escapedArgs.Replace("<","^<");
                    escapedArgs = escapedArgs.Replace(">","^>");
                    escapedArgs = escapedArgs.Replace("&","^&");
                }
                finalArguments = $"/c \"\"{debugExe}\" {escapedArgs} & pause\"";
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
        private async Task<DebugLaunchSettings> GetConsoleTargetForProfile(ILaunchProfile resolvedProfile, DebugLaunchOptions launchOptions, 
                                                              string projectFolder, bool useCmdShell)
        {
            var settings = new DebugLaunchSettings(launchOptions);

            string executable, arguments;
            string commandLineArguments = resolvedProfile.CommandLineArgs;

            // Is this profile just running the project? If so we ignore the exe
            if(string.Equals(resolvedProfile.CommandName, LaunchSettingsProvider.RunProjectCommandName, StringComparison.OrdinalIgnoreCase))
            {
                // Can't run a class library directly
                if(await GetIsClassLibraryAsync().ConfigureAwait(false))
                {
                    throw new Exception(VSResources.ProjectNotRunnableDirectly);
                }
                
                // The executable will be based on the output path. If the output is an exe we
                // just run that, otherwise, we need to launch dotnet.exe with the dll as its arguments
                var runData = await GetRunnableProjectInformationAsync().ConfigureAwait(false);
                executable = runData.Item1;
                arguments = runData.Item2;

                if(!string.IsNullOrWhiteSpace(resolvedProfile.CommandLineArgs))
                {                 
                    arguments = arguments + " " + resolvedProfile.CommandLineArgs;
                }
            }
            else
            {
                executable = resolvedProfile.ExecutablePath;
                arguments = resolvedProfile.CommandLineArgs;
            }

            string workingDir;
            if(string.IsNullOrWhiteSpace(resolvedProfile.WorkingDirectory))
            {
                workingDir = projectFolder;
            }
            else
            {
                // If the working directory is not rooted we assume it is relative to the project directory
                if(Path.IsPathRooted(resolvedProfile.WorkingDirectory))
                {
                    workingDir = resolvedProfile.WorkingDirectory;
                }
                else
                {
                    workingDir = Path.GetFullPath(Path.Combine(projectFolder, resolvedProfile.WorkingDirectory));
                }
            }

            // IF the executable is not rooted, we want to make is relative to the workingDir unless is doesn't contain
            // any path elements. In that case we are going to assume it is on the path
            if(!string.IsNullOrWhiteSpace(executable))
            {
                if (!Path.IsPathRooted(executable) && executable.IndexOf(Path.DirectorySeparatorChar) != -1)
                {
                    executable = Path.GetFullPath(Path.Combine(workingDir, executable));
                }
            }

            // Now validate the executable path and working directory exist
            ValidateSettings(executable, workingDir, resolvedProfile.Name);
            
            // Now get final exe and args. CTtrl-F5 wraps exe in cmd prompt
            string finalExecutable, finalArguments;
            GetExeAndArguments(useCmdShell, executable, arguments, out finalExecutable, out finalArguments);


            // Apply environment variables.
            if (resolvedProfile.EnvironmentVariables != null && resolvedProfile.EnvironmentVariables.Count > 0)
            {
                foreach(var kvp in resolvedProfile.EnvironmentVariables)
                {
                    settings.Environment[kvp.Key] = kvp.Value;
                }
            }

            settings.Executable = finalExecutable;
            settings.Arguments = finalArguments;
            settings.CurrentDirectory = workingDir;
            settings.LaunchOperation = DebugLaunchOperation.CreateProcess;
            settings.LaunchDebugEngineGuid = await GetDebuggingEngineAsync().ConfigureAwait(false);
            settings.LaunchOptions = launchOptions | DebugLaunchOptions.StopDebuggingOnEnd;
            if(settings.Environment.Count > 0)
            {
                settings.LaunchOptions = settings.LaunchOptions | DebugLaunchOptions.MergeEnvironment;
            }

            return settings;
        }

        private async Task<Tuple<string, string>> GetRunnableProjectInformationAsync()
        {
            var properties = ConfiguredProject.Services.ProjectPropertiesProvider.GetCommonProperties();

            // Assumes dotnet.exe is on the the path. This will be replaced by accessing a property from the project file
            // TODO: tracked by issue https://github.com/dotnet/roslyn-project-system/issues/423
            string executable, arguments;
            var targetPath = await properties.GetEvaluatedPropertyValueAsync("TargetPath").ConfigureAwait(false);
            if (targetPath.EndsWith(".exe"))
            {
                executable = targetPath;
                arguments = "";
            }
            else
            {
                executable = GetFullPathOfExeFromEnvironmentPath("dotnet.exe");  // await properties.GetEvaluatedPropertyValueAsync("ExeToLaunch").ConfigureAwait(false);
                if (executable == null)
                {
                    executable = "dotnet.exe";
                }

                arguments = targetPath.QuoteString();
            }

            return new Tuple<string, string>(executable, arguments);
        }

        private async Task<Guid> GetDebuggingEngineAsync()
        {
            var properties = ConfiguredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
            var framework = await properties.GetEvaluatedPropertyValueAsync("TargetFrameworkIdentifier").ConfigureAwait(false);

            return ProjectDebuggerProvider.GetManagedDebugEngineForFramework(framework);
        }

        /// <summary>
        /// Searchs the path variable for the first match of exeToSearchFor. Returns 
        /// null if not found. 
        /// </summary>
        public string GetFullPathOfExeFromEnvironmentPath(string exeToSearchFor)
        {
            string pathEnv = TheEnvironment.GetEnvironmentVariable("Path");

            if (string.IsNullOrEmpty(pathEnv))
            {
                return null;
            }

            var paths = pathEnv.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                // We don't want one bad path entry to derail the search
                try
                {
                    string exePath = Path.Combine(path, exeToSearchFor);
                    if(TheFileSystem.FileExists(exePath))
                    {
                        return exePath;
                    }
                }
                catch(ArgumentException)
                {
                }
            }

            return null;
        }
    }
}
