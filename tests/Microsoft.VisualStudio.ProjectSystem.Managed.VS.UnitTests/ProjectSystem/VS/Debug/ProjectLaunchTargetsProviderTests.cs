﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Debugger.UI.Interfaces.HotReload;
using Microsoft.VisualStudio.IO;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.HotReload;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    public class ProjectLaunchTargetsProviderTests
    {
        private readonly string _ProjectFile = @"c:\test\project\project.csproj";
        private readonly string _Path = @"c:\program files\dotnet;c:\program files\SomeDirectory";
        private readonly IFileSystemMock _mockFS = new();

        [Fact]
        public void GetExeAndArguments()
        {
            string exeIn = @"c:\foo\bar.exe";
            string argsIn = "/foo /bar";
            string cmdExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            ProjectLaunchTargetsProvider.GetExeAndArguments(false, exeIn, argsIn, out string? finalExePath, out string? finalArguments);
            Assert.Equal(finalExePath, exeIn);
            Assert.Equal(finalArguments, argsIn);

            ProjectLaunchTargetsProvider.GetExeAndArguments(true, exeIn, argsIn, out finalExePath, out finalArguments);
            Assert.Equal(cmdExePath, finalExePath);
            Assert.Equal("/c \"\"c:\\foo\\bar.exe\" /foo /bar & pause\"", finalArguments);
        }

        [Fact]
        public void GetExeAndArgumentsWithEscapedArgs()
        {
            string exeIn = @"c:\foo\bar.exe";
            string argsInWithEscapes = "/foo /bar ^ < > &";
            string cmdExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            ProjectLaunchTargetsProvider.GetExeAndArguments(true, exeIn, argsInWithEscapes, out string? finalExePath, out string? finalArguments);
            Assert.Equal(cmdExePath, finalExePath);
            Assert.Equal("/c \"\"c:\\foo\\bar.exe\" /foo /bar ^^ ^< ^> ^& & pause\"", finalArguments);

            ProjectLaunchTargetsProvider.GetExeAndArguments(false, exeIn, argsInWithEscapes, out finalExePath, out finalArguments);
            Assert.Equal(exeIn, finalExePath);
            Assert.Equal(argsInWithEscapes, finalArguments);
        }

        [Fact]
        public void GetExeAndArgumentsWithNullArgs()
        {
            string exeIn = @"c:\foo\bar.exe";
            string cmdExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            ProjectLaunchTargetsProvider.GetExeAndArguments(true, exeIn, "", out string? finalExePath, out string? finalArguments);
            Assert.Equal(cmdExePath, finalExePath);
            Assert.Equal("/c \"\"c:\\foo\\bar.exe\"  & pause\"", finalArguments);
        }

        [Fact]
        public void GetExeAndArgumentsWithEmptyArgs()
        {
            string exeIn = @"c:\foo\bar.exe";
            string cmdExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            // empty string args
            ProjectLaunchTargetsProvider.GetExeAndArguments(true, exeIn, "", out string? finalExePath, out string? finalArguments);
            Assert.Equal(cmdExePath, finalExePath);
            Assert.Equal("/c \"\"c:\\foo\\bar.exe\"  & pause\"", finalArguments);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ProjectProfileAsyncF5()
        {
            var debugger = GetDebugTargetsProvider();

            await _mockFS.WriteAllTextAsync(@"c:\program files\dotnet\dotnet.exe", "");
            _mockFS.CreateDirectory(@"c:\test\project");

            var activeProfile = new LaunchProfile("MyApplication", "Project", commandLineArgs: "--someArgs");
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"c:\program files\dotnet\dotnet.exe", targets[0].Executable);
            Assert.Equal(DebugLaunchOperation.CreateProcess, targets[0].LaunchOperation);
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, targets[0].LaunchDebugEngineGuid);
            Assert.Empty(targets[0].AdditionalDebugEngines);
            Assert.Equal("exec \"c:\\test\\project\\bin\\project.dll\" --someArgs", targets[0].Arguments);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ProjectProfileAsyncF5_NativeDebugging()
        {
            var debugger = GetDebugTargetsProvider();

            await _mockFS.WriteAllTextAsync(@"c:\program files\dotnet\dotnet.exe", "");
            _mockFS.CreateDirectory(@"c:\test\project");

            var activeProfile = new LaunchProfile(
                name: "MyApplication",
                commandName: "Project",
                commandLineArgs: "--someArgs",
                otherSettings: ImmutableArray.Create<(string, object)>((LaunchProfileExtensions.NativeDebuggingProperty, (object)true)));
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"c:\program files\dotnet\dotnet.exe", targets[0].Executable);
            Assert.Equal(DebugLaunchOperation.CreateProcess, targets[0].LaunchOperation);
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, targets[0].LaunchDebugEngineGuid);
            Assert.Single(targets[0].AdditionalDebugEngines);
            Assert.Equal(DebuggerEngines.NativeOnlyEngine, targets[0].AdditionalDebugEngines[0]);
            Assert.Equal("exec \"c:\\test\\project\\bin\\project.dll\" --someArgs", targets[0].Arguments);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ProjectProfileAsyncCtrlF5()
        {
            var debugger = GetDebugTargetsProvider();

            var activeProfile = new LaunchProfile(
                name: "MyApplication",
                commandName: "Project",
                commandLineArgs: "--someArgs",
                environmentVariables: ImmutableArray.Create(("var1", "Value1")));

            var targets = await debugger.QueryDebugTargetsAsync(DebugLaunchOptions.NoDebug, activeProfile);
            Assert.Single(targets);
            Assert.EndsWith(@"\cmd.exe", targets[0].Executable, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(DebugLaunchOperation.CreateProcess, targets[0].LaunchOperation);
            Assert.Equal(DebugLaunchOptions.NoDebug | DebugLaunchOptions.MergeEnvironment, targets[0].LaunchOptions);
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, targets[0].LaunchDebugEngineGuid);
            Assert.True(targets[0].Environment.ContainsKey("var1"));
            Assert.Equal("/c \"\"c:\\program files\\dotnet\\dotnet.exe\" exec \"c:\\test\\project\\bin\\project.dll\" --someArgs & pause\"", targets[0].Arguments);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ProjectProfileAsyncProfile()
        {
            var debugger = GetDebugTargetsProvider();

            var activeProfile = new LaunchProfile("MyApplication", "Project", commandLineArgs: "--someArgs");

            // Validate that when the DLO_Profiling is set we don't run the cmd.exe
            var targets = await debugger.QueryDebugTargetsAsync(DebugLaunchOptions.NoDebug | DebugLaunchOptions.Profiling, activeProfile);
            Assert.Single(targets);
            Assert.Equal("c:\\program files\\dotnet\\dotnet.exe", targets[0].Executable);
            Assert.Equal(DebugLaunchOptions.NoDebug | DebugLaunchOptions.Profiling, targets[0].LaunchOptions);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ExeProfileAsyncF5()
        {
            var debugger = GetDebugTargetsProvider();

            var activeProfile = new LaunchProfile(
                "MyApplication",
                commandName: null,
                commandLineArgs: "--someArgs",
                executablePath: @"c:\test\Project\someapp.exe");
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(activeProfile.ExecutablePath, targets[0].Executable);
            Assert.Equal(DebugLaunchOperation.CreateProcess, targets[0].LaunchOperation);
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, targets[0].LaunchDebugEngineGuid);
            Assert.Equal("--someArgs", targets[0].Arguments);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ExeProfileAsyncCtrlF5()
        {
            var debugger = GetDebugTargetsProvider();

            var activeProfile = new LaunchProfile(
                name: "MyApplication",
                commandName: null,
                commandLineArgs: "--someArgs",
                executablePath: @"c:\test\Project\someapp.exe",
                environmentVariables: ImmutableArray.Create(("var1", "Value1")));
            var targets = await debugger.QueryDebugTargetsAsync(DebugLaunchOptions.NoDebug, activeProfile);
            Assert.Single(targets);
            Assert.Equal(activeProfile.ExecutablePath, targets[0].Executable);
            Assert.Equal(DebugLaunchOperation.CreateProcess, targets[0].LaunchOperation);
            Assert.Equal(DebugLaunchOptions.NoDebug | DebugLaunchOptions.MergeEnvironment, targets[0].LaunchOptions);
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, targets[0].LaunchDebugEngineGuid);
            Assert.Equal("--someArgs", targets[0].Arguments);
        }

        [Theory]
        [InlineData(@"c:\test\project\bin\")]
        [InlineData(@"bin\")]
        [InlineData(@"doesntExist\")]
        [InlineData(null)]
        public async Task QueryDebugTargetsAsync_ExeProfileAsyncExeRelativeNoWorkingDir(string? outdir)
        {
            var properties = new Dictionary<string, string?>
            {
                { "RunCommand", @"dotnet" },
                { "RunArguments", "exec " + "\"" + @"c:\test\project\bin\project.dll"+ "\"" },
                { "RunWorkingDirectory",  @"bin\" },
                { "TargetFrameworkIdentifier", @".NetCoreApp" },
                { "OutDir", outdir }
            };

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Console, properties);

            // Exe relative, no working dir
            await _mockFS.WriteAllTextAsync(@"c:\test\project\bin\test.exe", string.Empty);
            await _mockFS.WriteAllTextAsync(@"c:\test\project\test.exe", string.Empty);
            var activeProfile = new LaunchProfile("run", null, executablePath: ".\\test.exe");
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            if (outdir is null || outdir == @"doesntExist\")
            {
                Assert.Equal(@"c:\test\project\test.exe", targets[0].Executable);
                Assert.Equal(@"c:\test\project", targets[0].CurrentDirectory);
            }
            else
            {
                Assert.Equal(@"c:\test\project\bin\test.exe", targets[0].Executable);
                Assert.Equal(@"c:\test\project\bin\", targets[0].CurrentDirectory);
            }
        }

        [Theory]
        [InlineData(@"c:\WorkingDir")]
        [InlineData(@"\WorkingDir")]
        public async Task QueryDebugTargetsAsync_ExeProfileAsyncExeRelativeToWorkingDir(string workingDir)
        {
            var debugger = GetDebugTargetsProvider();

            // Exe relative to full working dir
            await _mockFS.WriteAllTextAsync(@"c:\WorkingDir\mytest.exe", string.Empty);
            _mockFS.SetCurrentDirectory(@"c:\Test");
            _mockFS.CreateDirectory(@"c:\WorkingDir");
            var activeProfile = new LaunchProfile("run", null, executablePath: ".\\mytest.exe", workingDirectory: workingDir);
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"c:\WorkingDir\mytest.exe", targets[0].Executable);
            Assert.Equal(@"c:\WorkingDir", targets[0].CurrentDirectory);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ExeProfileAsyncExeRelativeToWorkingDir_AlternateSlash()
        {
            var debugger = GetDebugTargetsProvider();

            // Exe relative to full working dir
            await _mockFS.WriteAllTextAsync(@"c:\WorkingDir\mytest.exe", string.Empty);
            _mockFS.CreateDirectory(@"c:\WorkingDir");
            var activeProfile = new LaunchProfile("run", null, executablePath: "./mytest.exe", workingDirectory: @"c:/WorkingDir");
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"c:\WorkingDir\mytest.exe", targets[0].Executable);
            Assert.Equal(@"c:\WorkingDir", targets[0].CurrentDirectory);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_SetsProject()
        {
            var project = new LaunchProfile("run", null, executablePath: "dotnet.exe");

            var debugger = GetDebugTargetsProvider();
            var targets = await debugger.QueryDebugTargetsAsync(0, project);

            Assert.Single(targets);
            Assert.NotNull(targets[0].Project);
        }

        [Theory]
        [InlineData("dotnet")]
        [InlineData("dotnet.exe")]
        public async Task QueryDebugTargetsAsync_ExeProfileExeRelativeToPath(string exeName)
        {
            var debugger = GetDebugTargetsProvider();

            // Exe relative to path
            var activeProfile = new LaunchProfile("run", null, executablePath: exeName);
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"c:\program files\dotnet\dotnet.exe", targets[0].Executable);
        }

        [Theory]
        [InlineData("myexe")]
        [InlineData("myexe.exe")]
        public async Task QueryDebugTargetsAsync_ExeProfileExeRelativeToCurrentDirectory(string exeName)
        {
            var debugger = GetDebugTargetsProvider();
            await _mockFS.WriteAllTextAsync(@"c:\CurrentDirectory\myexe.exe", string.Empty);
            _mockFS.SetCurrentDirectory(@"c:\CurrentDirectory");

            // Exe relative to path
            var activeProfile = new LaunchProfile("run", null, executablePath: exeName);
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"c:\CurrentDirectory\myexe.exe", targets[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ExeProfileExeIsRootedWithNoDrive()
        {
            var debugger = GetDebugTargetsProvider();
            await _mockFS.WriteAllTextAsync(@"e:\myexe.exe", string.Empty);
            _mockFS.SetCurrentDirectory(@"e:\CurrentDirectory");

            // Exe relative to path
            var activeProfile = new LaunchProfile("run", null, executablePath: @"\myexe.exe");
            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);
            Assert.Single(targets);
            Assert.Equal(@"e:\myexe.exe", targets[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_WhenLibraryWithRunCommand_ReturnsRunCommand()
        {
            var properties = new Dictionary<string, string?>
            {
                {"RunCommand", @"C:\dotnet.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Library, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);

            Assert.Single(targets);
            Assert.Equal(@"C:\dotnet.exe", targets[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_WhenLibraryWithoutRunCommand_ReturnsTargetPath()
        {
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\library.dll"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Library, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);

            Assert.Single(targets);
            Assert.Equal(@"C:\library.dll", targets[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_WhenLibraryWithoutRunCommand_DoesNotManipulateTargetPath()
        {
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"library.dll"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Library, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);

            Assert.Single(targets);
            Assert.Equal(@"library.dll", targets[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsForDebugLaunchAsync_WhenLibraryAndNoRunCommandSpecified_Throws()
        {
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\library.dll"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            await _mockFS.WriteAllTextAsync(@"C:\library.dll", string.Empty);

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Library, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            await Assert.ThrowsAsync<Exception>(() => debugger.QueryDebugTargetsForDebugLaunchAsync(0, activeProfile));
        }

        [Fact]
        public async Task QueryDebugTargetsForDebugLaunchAsync_WhenLibraryAndRunCommandSpecified_ReturnsRunCommand()
        {
            var properties = new Dictionary<string, string?>
            {
                {"RunCommand", @"C:\dotnet.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            await _mockFS.WriteAllTextAsync(@"C:\library.dll", string.Empty);

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Library, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            var targets = await debugger.QueryDebugTargetsAsync(0, activeProfile);

            Assert.Single(targets);
            Assert.Equal(@"C:\dotnet.exe", targets[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ConsoleAppLaunchWithNoDebugger_WrapsInCmd()
        {
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\ConsoleApp.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var debugger = GetDebugTargetsProvider(ProjectOutputType.Console, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            var result = await debugger.QueryDebugTargetsAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.Single(result);
            Assert.Contains("cmd.exe", result[0].Executable);
        }

        [Fact]
        public async Task QueryDebugTargetsAsync_ConsoleAppLaunchWithNoDebuggerWithIntegratedConsoleEnabled_DoesNotWrapInCmd()
        {
            var debugger = IVsDebugger10Factory.ImplementIsIntegratedConsoleEnabled(enabled: true);
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\ConsoleApp.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var scope = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapabilities.IntegratedConsoleDebugging });

            var provider = GetDebugTargetsProvider(ProjectOutputType.Console, properties, debugger, scope);

            var activeProfile = new LaunchProfile("Name", "Project");

            var result = await provider.QueryDebugTargetsAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.Single(result);
            Assert.DoesNotContain("cmd.exe", result[0].Executable);
        }

        [Theory]
        [InlineData((DebugLaunchOptions)0)]
        [InlineData(DebugLaunchOptions.NoDebug)]
        public async Task QueryDebugTargetsAsync_ConsoleAppLaunch_IncludesIntegratedConsoleInLaunchOptions(DebugLaunchOptions launchOptions)
        {
            var debugger = IVsDebugger10Factory.ImplementIsIntegratedConsoleEnabled(enabled: true);
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\ConsoleApp.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var scope = IProjectCapabilitiesScopeFactory.Create(capabilities: new string[] { ProjectCapabilities.IntegratedConsoleDebugging });

            var provider = GetDebugTargetsProvider(ProjectOutputType.Console, properties, debugger, scope);

            var activeProfile = new LaunchProfile("Name", "Project");

            var result = await provider.QueryDebugTargetsAsync(launchOptions, activeProfile);

            Assert.Single(result);
            Assert.True((result[0].LaunchOptions & DebugLaunchOptions.IntegratedConsole) == DebugLaunchOptions.IntegratedConsole);
        }

        [Theory]
        [InlineData((DebugLaunchOptions)0)]
        [InlineData(DebugLaunchOptions.NoDebug)]
        public async Task QueryDebugTargetsAsync_NonIntegratedConsoleCapability_DoesNotIncludeIntegrationConsoleInLaunchOptions(DebugLaunchOptions launchOptions)
        {
            var debugger = IVsDebugger10Factory.ImplementIsIntegratedConsoleEnabled(enabled: true);
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\ConsoleApp.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var provider = GetDebugTargetsProvider(ProjectOutputType.Console, properties, debugger);

            var activeProfile = new LaunchProfile("Name", "Project");

            var result = await provider.QueryDebugTargetsAsync(launchOptions, activeProfile);

            Assert.Single(result);
            Assert.True((result[0].LaunchOptions & DebugLaunchOptions.IntegratedConsole) != DebugLaunchOptions.IntegratedConsole);
        }

        [Theory]
        [InlineData(ProjectOutputType.Library)]
        [InlineData(ProjectOutputType.Other)]
        public async Task QueryDebugTargetsAsync_NonConsoleAppLaunch_DoesNotIncludeIntegrationConsoleInLaunchOptions(ProjectOutputType outputType)
        {
            var debugger = IVsDebugger10Factory.ImplementIsIntegratedConsoleEnabled(enabled: true);
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\ConsoleApp.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var provider = GetDebugTargetsProvider(outputType, properties, debugger);

            var activeProfile = new LaunchProfile("Name", "Project");

            var result = await provider.QueryDebugTargetsAsync(0, activeProfile);

            Assert.Single(result);
            Assert.True((result[0].LaunchOptions & DebugLaunchOptions.IntegratedConsole) != DebugLaunchOptions.IntegratedConsole);
        }

        [Theory]
        [InlineData(ProjectOutputType.Library)]
        [InlineData(ProjectOutputType.Other)]
        public async Task QueryDebugTargetsAsync_NonConsoleAppLaunchWithNoDebugger_DoesNotWrapInCmd(ProjectOutputType outputType)
        {
            var properties = new Dictionary<string, string?>
            {
                {"TargetPath", @"C:\ConsoleApp.exe"},
                {"TargetFrameworkIdentifier", @".NETFramework"}
            };

            var debugger = GetDebugTargetsProvider(outputType, properties);

            var activeProfile = new LaunchProfile("Name", "Project");

            var result = await debugger.QueryDebugTargetsAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.Single(result);
            Assert.Equal(@"C:\ConsoleApp.exe", result[0].Executable);
        }

        [Fact]
        public void ValidateSettings_WhenNoExe_Throws()
        {
            string? executable = null;
            string? workingDir = null;
            var debugger = GetDebugTargetsProvider();
            var profileName = "run";

            Assert.ThrowsAny<Exception>(() =>
            {
                debugger.ValidateSettings(executable!, workingDir!, profileName);
            });
        }

        [Fact]
        public void ValidateSettings_WhenExeNotFoundThrows()
        {
            string executable = @"c:\foo\bar.exe";
            string? workingDir = null;
            var debugger = GetDebugTargetsProvider();
            var profileName = "run";

            Assert.ThrowsAny<Exception>(() =>
            {
                debugger.ValidateSettings(executable, workingDir!, profileName);
            });
        }

        [Fact]
        public void ValidateSettings_WhenExeFound_DoesNotThrow()
        {
            string executable = @"c:\foo\bar.exe";
            string? workingDir = null;
            var debugger = GetDebugTargetsProvider();
            var profileName = "run";
            _mockFS.Create(executable);

            debugger.ValidateSettings(executable, workingDir!, profileName);
            Assert.True(true);
        }

        [Fact]
        public void ValidateSettings_WhenWorkingDirNotFound_Throws()
        {
            string executable = "bar.exe";
            string workingDir = "c:\foo";
            var debugger = GetDebugTargetsProvider();
            var profileName = "run";

            Assert.ThrowsAny<Exception>(() =>
            {
                debugger.ValidateSettings(executable, workingDir, profileName);
            });
        }

        [Fact]
        public async Task CommandLineArgNewLines_AreStripped()
        {
            var provider = GetDebugTargetsProvider(
                outputType: ProjectOutputType.Library,
                properties: new Dictionary<string, string?>(),
                debugger: null,
                scope: null);

            var activeProfile = new LaunchProfile("Name", "Executable", commandLineArgs: "-arg1\r\n-arg2 -arg3\n -arg4\r\n\\r\\n");
            var launchSettings = await provider.GetConsoleTargetForProfileAsync(activeProfile, DebugLaunchOptions.NoDebug, false);

            Assert.Equal("-arg1 -arg2 -arg3  -arg4 \\r\\n", launchSettings?.Arguments);

        }

        [Fact]
        public void ValidateSettings_WhenWorkingDirFound_DoesNotThrow()
        {
            string executable = "bar.exe";
            string workingDir = "c:\foo";
            var debugger = GetDebugTargetsProvider();
            var profileName = "run";

            _mockFS.AddFolder(workingDir);

            debugger.ValidateSettings(executable, workingDir, profileName);
            Assert.True(true);
        }

        [Theory]
        [InlineData("exec \"C:\\temp\\test.dll\"", "exec \"C:\\temp\\test.dll\"")]
        [InlineData("exec ^<>\"C:\\temp&^\\test.dll\"&", "exec ^^^<^>\"C:\\temp&^\\test.dll\"^&")]
        public void ConsoleDebugTargetsProvider_EscapeString_WorksCorrectly(string input, string expected)
        {
            Assert.Equal(expected, ProjectLaunchTargetsProvider.EscapeString(input, new[] { '^', '<', '>', '&' }));
        }

        [Fact]
        public void GetDebugEngineForFrameworkTests()
        {
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectLaunchTargetsProvider.GetManagedDebugEngineForFramework(".NetStandardApp"));
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectLaunchTargetsProvider.GetManagedDebugEngineForFramework(".NetStandard"));
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectLaunchTargetsProvider.GetManagedDebugEngineForFramework(".NetCore"));
            Assert.Equal(DebuggerEngines.ManagedCoreEngine, ProjectLaunchTargetsProvider.GetManagedDebugEngineForFramework(".NetCoreApp"));
            Assert.Equal(DebuggerEngines.ManagedOnlyEngine, ProjectLaunchTargetsProvider.GetManagedDebugEngineForFramework(".NETFramework"));
        }

        [Fact]
        public async Task CanBeStartupProject_WhenUsingExecutableCommand_AlwaysTrue()
        {
            var provider = GetDebugTargetsProvider(
                outputType: ProjectOutputType.Library,
                properties: new Dictionary<string, string?>(),
                debugger: null,
                scope: null);

            var activeProfile = new LaunchProfile("Name", "Executable");
            bool canBeStartupProject = await provider.CanBeStartupProjectAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.True(canBeStartupProject);
        }

        [Fact]
        public async Task CanBeStartupProject_WhenUsingProjectCommand_TrueIfRunCommandPropertySpecified()
        {
            var provider = GetDebugTargetsProvider(
                properties: new Dictionary<string, string?>() { { "RunCommand", @"C:\alpha\beta\gamma.exe" } });

            var activeProfile = new LaunchProfile("Name", "Project");
            bool canBeStartupProject = await provider.CanBeStartupProjectAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.True(canBeStartupProject);
        }

        [Fact]
        public async Task CanBeStartupProject_WhenUsingProjectCommand_TrueIfTargetPathPropertySpecified()
        {
            var provider = GetDebugTargetsProvider(
                properties: new Dictionary<string, string?>() { { "TargetPath", @"C:\alpha\beta\gamma.exe" } });

            var activeProfile = new LaunchProfile("Name", "Project");
            bool canBeStartupProject = await provider.CanBeStartupProjectAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.True(canBeStartupProject);
        }

        [Fact]
        public async Task CanBeStartupProject_WhenUsingProjectCommand_FalseIfRunCommandAndTargetPathNotSpecified()
        {
            var provider = GetDebugTargetsProvider(
                outputType: ProjectOutputType.Library,
                properties: new Dictionary<string, string?>(),
                debugger: null,
                scope: null);

            var activeProfile = new LaunchProfile("Name", "Project");
            bool canBeStartupProject = await provider.CanBeStartupProjectAsync(DebugLaunchOptions.NoDebug, activeProfile);

            Assert.False(canBeStartupProject);
        }

        private ProjectLaunchTargetsProvider GetDebugTargetsProvider(ProjectOutputType outputType = ProjectOutputType.Console, Dictionary<string, string?>? properties = null, IVsDebugger10? debugger = null, IProjectCapabilitiesScope? scope = null)
        {
            _mockFS.Create(@"c:\test\Project\someapp.exe");
            _mockFS.CreateDirectory(@"c:\test\Project");
            _mockFS.CreateDirectory(@"c:\test\Project\bin\");
            _mockFS.Create(@"c:\program files\dotnet\dotnet.exe");

            var project = UnconfiguredProjectFactory.Create(fullPath: _ProjectFile);

            var outputTypeChecker = outputType switch
            {
                ProjectOutputType.Console => IOutputTypeCheckerFactory.Create(isLibrary: false, isConsole: true),
                ProjectOutputType.Library => IOutputTypeCheckerFactory.Create(isLibrary: true, isConsole: false),
                ProjectOutputType.Other => IOutputTypeCheckerFactory.Create(isLibrary: false, isConsole: false),
                _ => throw new InvalidOperationException($"Unexpected {nameof(ProjectOutputType)}: {outputType}")
            };

            properties ??= new Dictionary<string, string?>
            {
                {"RunCommand", @"dotnet"},
                {"RunArguments", "exec " + "\"" + @"c:\test\project\bin\project.dll" + "\""},
                {"RunWorkingDirectory", @"bin\"},
                {"TargetFrameworkIdentifier", @".NetCoreApp"},
                {"OutDir", @"c:\test\project\bin\"}
            };

            var delegatePropertiesMock = IProjectPropertiesFactory.MockWithPropertiesAndValues(properties);

            var delegateProvider = IProjectPropertiesProviderFactory.Create(null, delegatePropertiesMock.Object);

            var configuredProjectServices = Mock.Of<ConfiguredProjectServices>(o =>
                o.ProjectPropertiesProvider == delegateProvider);

            var capabilitiesScope = scope ?? IProjectCapabilitiesScopeFactory.Create(capabilities: Enumerable.Empty<string>());

            var configuredProject = Mock.Of<ConfiguredProject>(o =>
                o.UnconfiguredProject == project &&
                o.Services == configuredProjectServices &&
                o.Capabilities == capabilitiesScope);
            var environment = IEnvironmentHelperFactory.ImplementGetEnvironmentVariable(_Path);

            return CreateInstance(configuredProject: configuredProject, fileSystem: _mockFS, typeChecker: outputTypeChecker, environment: environment, debugger: debugger);
        }

        private static ProjectLaunchTargetsProvider CreateInstance(
            ConfiguredProject? configuredProject = null,
            IDebugTokenReplacer? tokenReplacer = null,
            IFileSystem? fileSystem = null,
            IEnvironmentHelper? environment = null,
            IActiveDebugFrameworkServices? activeDebugFramework = null,
            IOutputTypeChecker? typeChecker = null,
            IProjectThreadingService? threadingService = null,
            IVsDebugger10? debugger = null,
            IHotReloadOptionService? hotReloadSettings = null)
        {
            environment ??= Mock.Of<IEnvironmentHelper>();
            tokenReplacer ??= IDebugTokenReplacerFactory.Create();
            activeDebugFramework ??= IActiveDebugFrameworkServicesFactory.ImplementGetConfiguredProjectForActiveFrameworkAsync(configuredProject);
            threadingService ??= IProjectThreadingServiceFactory.Create();
            debugger ??= IVsDebugger10Factory.ImplementIsIntegratedConsoleEnabled(enabled: false);
            typeChecker ??= IOutputTypeCheckerFactory.Create();

            IUnconfiguredProjectVsServices unconfiguredProjectVsServices = IUnconfiguredProjectVsServicesFactory.Implement(IVsHierarchyFactory.Create);

            IRemoteDebuggerAuthenticationService remoteDebuggerAuthenticationService = Mock.Of<IRemoteDebuggerAuthenticationService>();

            return new ProjectLaunchTargetsProvider(
                unconfiguredProjectVsServices,
                configuredProject!,
                tokenReplacer,
                fileSystem!,
                environment,
                activeDebugFramework,
                typeChecker,
                threadingService,
                IVsUIServiceFactory.Create<SVsShellDebugger, IVsDebugger10>(debugger),
                remoteDebuggerAuthenticationService,
                new Lazy<IProjectHotReloadSessionManager>(IProjectHotReloadSessionManagerFactory.Create),
                new Lazy<IHotReloadOptionService>(() => hotReloadSettings ?? IHotReloadOptionServiceFactory.Create()));
        }

        /// <summary>
        /// Indicates if the project is a library, console app, or something else.
        /// </summary>
        public enum ProjectOutputType
        {
            Library,
            Console,
            Other
        }
    }
}
