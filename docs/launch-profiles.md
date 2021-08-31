# Launch Profiles

The _Launch Profiles_ feature of the .NET Project System allows developers to specify multiple ways of running their project, each with a different set of options, and quickly switch between them in Visual Studio. For example, when developing a web application you may have one profile that runs your project under the Kestrel web server, and another that launches it under IIS Express. Or, when creating a console application, different profiles may specify different command line arguments.

## Example

The launch profiles for a particular project (if any) are stored in the Properties\launchSettings.json file. Below is an example based on a launchSettings.json found in this repo. Note that within Visual Studio it is more common to define and update launch profiles through the UI (via the project properties in VS2019 and earlier, or the Launch Profiles UI in VS2022 and later) but the file can be edited directly.

``` json
{
  "profiles": {
    "Start (with native debugging)": {
      "commandName": "Executable",
      "executablePath": "$(DevEnvDir)devenv.exe",
      "commandLineArgs": "/rootsuffix $(VSSDKTargetPlatformRegRootSuffix) /log",
      "environmentVariables": {
        "VisualBasicDesignTimeTargetsPath": "$(VisualStudioXamlRulesDir)Microsoft.VisualBasic.DesignTime.targets",
        "FSharpDesignTimeTargetsPath": "$(VisualStudioXamlRulesDir)Microsoft.FSharp.DesignTime.targets",
        "CSharpDesignTimeTargetsPath": "$(VisualStudioXamlRulesDir)Microsoft.CSharp.DesignTime.targets",
        "CPS_DiagnosticRuntime": "1",
        "CPS_MetricsCollection": "1"
      },
      "nativeDebugging": true
    }
  }
}
```

This file contains a single launch profile named "Start (with native debugging)" containing various settings:

- `commandName`: This is the most important setting, and the only required one. We'll come back to it in a moment.
- `executablePath`: The path to an executable (.exe) to run when this launch profile is selected, and the project is "started". In this starting the project will run devenv.exe.
- `commandLineArgs`: The arguments to supply to the specified executable.
- `environmentVariables`: A collection of name/value pairs, each specifying an environment variable and value to set. A setting in a profile can contain any arbitrary data so long as it can be represented in JSON (and even that isn't always a requirement; see "In-memory settings and profiles", below).
- `nativeDebugging`: Specifies that the native code debugging engine should be used in addition to the managed code debugging engine.

This is not an exhaustive list of settings, as we will discuss below.

## Launch Commands

The most important setting in the launch profile is the `commandName` which specifies the _launch command_ associated with that profile.

> The launch command is often referred to as a "launch target", "debug command", or "debug target". As launch profiles are not restricted to debugging, and the term "target" has a specific and unrelated meaning within the project system, we will stick with "launch command".

The launch command specifies which VS component is responsible for interpreting the active launch profile and taking whatever actions are appropriate when "starting" the project. The .NET Project System provides handlers for the "Executable" command (which, as seen in the example, runs a specified .exe) and the "Project" command, which is very similar but automatically chooses the binary produced by the project as the executable. The ASP.NET Core tooling adds handlers for the "IIS" and "IISExpress" commands, which run the project binary within a web server, and there are a number of others.

The important point is that launch profiles can contain arbitrary settings, and how to interpret those settings is entirely up to the command handler. By convention environment variables are stored in a setting named `environmentVariables`, but a particular command handler doesn't have to look for them there, or even support environment variables at all.

New launch commands can be defined by implementing the [`IDebugProfileLaunchTargetsProvider`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Debug/IDebugProfileLaunchTargetsProvider.cs) interface (and optionally `IDebugProfileLaunchTargetsProvider2`, and `IDebugProfileLaunchTargetsProvider3`). The "Project" and "Executable" commands are both handled by the [`ProjectLaunchTargetsProvider`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Debug/ProjectLaunchTargetsProvider.cs), for example.

## Representation in VS

Generally, the .NET Project System does not interact with the JSON directly. Instead, the launch profiles are managed as a set of [`ILaunchProfile`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/ILaunchProfile.cs) instances collected into an [`ILaunchSettings`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/ILaunchSettings.cs). The settings are in turn managed by the [`ILaunchSettingsProvider`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/ILaunchSettingsProvider.cs).

Certain commonly-used settings (such as the environment variables and command line arguments) are represented as individual properties on the `ILaunchProfile`; this encourages a certain level of consistency between profiles with different launch commands as the `ILaunchSettingsProvider` will take responsibility for serializing these to the JSON. Other settings can go in the dictionary accessed through the `OtherSettings` property.

Code wishing to add a profile MEF-imports the `ILaunchSettingsProvider` and calls `ILaunchSettingsProvider.AddOrUpdateProfileAsync`, passing in its own implementation of the `ILaunchProfile` interface. Similar methods exist for removing a profile.

## Features

### MSBuild property/environment variable substitution

The handler for the "Project" and "Executable" commands will automatically replace MSBuild property references (e.g. `$(MyProperty)`) and environment variable references (e.g. `%MY_VAR%`) with the corresponding values. In the above example this allows us to avoid a hard-coded path to devenv.exe, among other things.

This functionality is available to other launch commands through the [`IDebugTokenReplacer`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IDebugTokenReplacer.cs) interface.

### Command-specific UI

Each launch command can have its own associated UI for updating properties; see the [Project Property Pages documentation](https://github.com/dotnet/project-system/tree/main/docs/repo/property-pages) for more information.

### In-memory profiles and settings

VS supports in-memory profiles that are not committed to the launchSettings.json. For example, if no launchSettings.json is present the .NET Project System will automatically create an in-memory profile with the project's name and the "Project" launch command; this allows running a project without cluttering your project with a trivial launchSettings.json file. Only when the profile is edited through the UI do we create the file and commit the profile. Individual settings within a profile may also be in-memory only.

This is achieved by having your `ILaunchProfile` (or an individual setting's value) implement the optional [`IPersistOption`](https://github.com/dotnet/project-system/blob/main/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/IPersistOption.cs) interface.
