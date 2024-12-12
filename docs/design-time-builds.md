# Design-time builds

Visual Studio needs various information about your projects, including source files, references and options. In .NET, much of this data is defined in MSBuild `<Project>` files, such as `MyProject.csproj`. Visual Studio uses the MSBuild engine to obtain data about a project via a so-called _design-time build_ (DTB).

DTBs differ from normal builds in a few key ways:

- They are scheduled automatically by VS, such as during project load or when a project file changes.
- They run behind-the-scenes, without showing output in the IDE (generally).
- No compilation occurs, and outputs aren't modified.
- Additional MSBuild targets are invoked to obtain various data.

## Features that design-time builds support

Data obtained from DTBs drive many IDE features, including:

- **Language services** need to know things like `<LangVersion>`, source files, references, etc. This information drives syntax highlighting, IntelliSense, error highlighting, analyzers, etc.

- **NuGet** needs information in order to restore packages used by the project. Without this, types from NuGet packages are unavailable in the IDE and code that uses them will show errors. Packages can also contribute their own targets to DTBs to extend IDE behavior.

- **Dependencies tree** shows the project's declared dependencies, and the DTB attempts to resolve them. Any items that couldn't be resolved during the DTB are shown as warnings in the tree.

And many others.

## Design-time builds triggers

Design-time builds occur during project load, and in response to project changes. For example, changing the project's target framework, or adding a NuGet package, are both operations that modify the project (e.g. `.csproj`) file. Those changes trigger a design-time builds, which are scheduled behind the scenes.

Visual Studio caches data from these builds to disk in the `.vs` folder. During project load, if the cache is up to date, then the DTB is skipped and the cached data used instead.

If a project multi-targets (e.g. `<TargetFrameworks>net9.0;net48</TargetFrameworks>`) then a separate DTB is scheduled for each target framework. Data will differ between targets, so each needs its own DTB.

DTBs take the IDE's active configuration (e.g. _Debug_ or _Release_) into account. Changing the active configuration triggers a DTB.

When switching branches, DTBs only run if projects are modified.

Changing `Directory.*.props` and `Directory.*.targets` files will trigger DTBs in all nested projects.

The easiest way to manually trigger a DTB is to make a white-space change in the project file and save it. This can be helpful when capturing DTB logs (discussed below in [Diagnosing design-time builds](#Diagnosing-design-time-builds)).

## Problems with design-time builds

DTBs run in the IDE (i.e. at design time). A problematic DTB will manifest its problems in the IDE experience.

For example, a slow DTB will increase the time taken to load the project, and can hog resources when making various changes in the IDE.

DTBs can also report errors and fail. In Visual Studio, failing design-time builds display an info bar with text:

> An error occurred during a design-time build. Some features may not work correctly. See the Error List for details.

When a DTB fails, any number of VS features might fail to work. Some examples:

- IntelliSense shows incomplete and/or incorrect results.
- Code is not highlighted in the editor, or is highlighted incorrectly.
- Opening source files in the editor shows them in the `Miscellaneous Files` project.
- Builds succeed while outdated build failure messages are in the Error List.

## Diagnosing design-time builds

The error list will show any error messages from the DTB, however investigating errors and performance issues usually requires a build log.

### Capturing binlogs

To capture binary build logs (binlogs):

1. Open a _Developer Command Prompt_ for the version of Visual Studio you want to use.
1. Enable MSBuild logging via environment variable:
   ```
   set MSBUILDDEBUGENGINE=1
   ```
1. Decide where you want logs to be written:
   - By default, logs are written to a `MSBuild_Logs` subdirectory (beneath the current working directory).
   - To specify a directory to write logs, set the `MSBUILDDEBUGPATH` environment variable to an absolute directory path. It must be writeable by the current user.
      ```
      set MSBUILDDEBUGPATH=c:\some\path
      ```
1. Run `devenv.exe` to start Visual Studio.
1. Make a white-space change in the project you're investigating to trigger a DTB.
1. Open the directory from step 3 in Windows Explorer to see the captured `.binlog` and other diagnostic files.
1. Open the most recent `*DesignTimeBatchBuild*.binlog` file in the [MSBuild Structured Log Viewer](https://msbuildlog.com).

> [!NOTE]
> While this environment variable is set, all builds will be logged. You must manually remove these log files when they're no longer needed. It is recommended to only set this enviroment for short periods, as needed.

For more information, see:

- [Binary log format](https://github.com/dotnet/msbuild/blob/main/documentation/wiki/Binary-Log.md)
- [MSBuild Structured Log Viewer](https://msbuildlog.com)
- [Providing MSBuild binary logs for investigation](https://github.com/dotnet/msbuild/blob/main/documentation/wiki/Providing-Binary-Logs.md)
- [Further MSBuild documentation on binlogs](https://github.com/dotnet/msbuild/blob/main/documentation/wiki/Building-Testing-and-Debugging-on-Full-Framework-MSBuild.md#logs).

<details>
<summary>Instructions for versions before Visual Studio 2022</summary>

Support for `MSBUILDDEBUGENGINE` was added in MSBuild 17.0, which shipped with VS 17.0 (the first release of VS2022). Different approaches are needed for earlier versions of Visual Studio.

#### Visual Studio 2017

1. Install the [Project System Tools](https://github.com/dotnet/project-system-tools#project-system-tools) extension.
2. In Visual Studio, choose the `View > Other Windows > Build Logging` menu item.
3. Click on the "play" button.

This will cause design-time builds to show up in the build logging tool window. If you have the [MSBuild Structured Log Viewer](https://msbuildlog.com) installed, you can double-click on a log to view it in the viewer, otherwise you can right-click and choose `Save As...` to save a binary log.

Note that logs produced via this mechanism have lower verbosity than those captured by `MSBUILDDEBUGENGINE`

#### Visual Studio 2015 or below

1. Delete the `.vs` directory that sits alongside the solution that is experiencing the problem.
2. Start a _Developer Command Prompt for VS2015_.
3. At the prompt, run `set TRACEDESIGNTIME=true`
4. At the prompt, run `devenv`
5. Open the solution.
6. Under `%TEMP%`, look for `[RANDOMGUID].designtime.log` files, these will contain the results of the design-time build. If running Visual Studio 2015 Update 2 or higher, the name of the project and design-time target that is being called will also be included in the file name.

</details>

### Diagnosing failing design-time builds

When opening a `.binlog` in the [MSBuild Structured Log Viewer](https://msbuildlog.com), any error is immediately highlighted. If the cause of the error is not obvious, follow execution and data backwards until a cause is found. This will likely require at least a basic knowledge of MSBuild's evaluation and execution model.

### Diagnosing failing design-time builds

The [MSBuild Structured Log Viewer](https://msbuildlog.com) supports design-time build performance investigations.

Under a project's node, a _Top 10 most expensive tasks_ node lists the project's longest-running tasks and which targets invoked them.

There are also the _Timeline_ and _Tracing_ views that show execution duration in different forms.

## Designing targets for use in design-time builds

Targets that dynamically change references, source files or compilation options _must_ run during both normal _and_ design-time builds to avoid unexpected behavior in Visual Studio.

Equally, if a target does not contribute these items, then it should actively avoid running in these builds to ensure design-time builds are as fast as possible.

### Adding items during design-time builds

MSBuild targets that run during DTBs can add items to the project. To do so, the target must run before the corresponding design-time target, per the following table:

Design-Time Target                            | Normal Target                      | Description
----------------------------------------------|------------------------------------|------------------
ResolveAssemblyReferencesDesignTime           | ResolveAssemblyReferences          | Resolves `<Reference>` items to their paths.
ResolveProjectReferencesDesignTime            | ResolveProjectReferences           | Resolves `<ProjectReference>` items to their output paths.
ResolveComReferencesDesignTime                | ResolveComReferences               | Resolves `<COMReference>` items to their primary interop assemblies (PIA) paths.
ResolveFrameworkReferencesDesignTime          | ResolveFrameworkReferences         | Resolves `<FrameworkReference>` items to their paths.
ResolvePackageDependenciesDesignTime          | ResolvePackageDependencies         | Resolves `<PackageReference>` items to their paths.
CompileDesignTime (new project system)/Compile| Compile                            | Passes command-line arguments, `<Compile>` items and `<Analyzer>` items to the compiler in normal builds, or to the language service in design-time builds.

If you've determined that your target needs to run in a design-time build, refer to the above table and set `BeforeTargets` to the normal target equivalent of what you are contributing to the build. For example, if a target changes `<Reference>` items, then it should indicate that it runs _before_ `ResolveAssemblyReferences` target:

```xml
<Target Name="AddAdditionalReferences" BeforeTargets="ResolveAssemblyReferences">
    ...
</Target>
```

This new `AddAdditionalReferences` target will run in both normal builds _and_ design-time builds, leading to consistent results between them.

Note that dependencies added during DTBs will not be displayed in the the projects Dependencies tree in Solution Explorer. For an item to be shown as a dependency, it must be present during MSBuild evaluation.

### Determining whether a target is running in a design-time build

In rare cases, you may wish to stop a target from running during design-time builds. Perhaps the target performs expensive calculations that aren't required for Visual Studio. Skipping such targets helps keep the IDE responsive.

Different project systems use different properties to distinguish between design-time builds and normal builds. For example, the .NET Project System in this repo builds on top of the Common Project System (CPS), which sets the `DesignTimeBuild` property. Older, non-SDK-style C# and VB projects (from the .NET Framework era) use the `BuildingProject` property.

As such, you should make use of both the `DesignTimeBuild` and `BuildingProject` properties to determine whether a target is running in a design-time build or a normal build. Extending our earlier example:

```xml
<Target Name="AddAdditionalReferences" BeforeTargets="ResolveAssemblyReferences">
    <PropertyGroup Condition="'$(DesignTimeBuild)' == 'true' OR '$(BuildingProject)' != 'true'">
        <_AvoidExpensiveCalculation>true</_AvoidExpensiveCalculation>
    </PropertyGroup>
    ...
</Target>
```

> [!IMPORTANT]
> The `DesignTimeBuild` property is typically empty (`''`) in normal builds, so avoid comparisons to `'false'`.
 
### Specifying explicit dependencies

If your target has dependencies on properties, items or files produced during the build, it must have a `DependsOnTargets` attribute that accurately indicates the set of targets that produce those assets. An under-specified `DependsOnTargets` will lead to unexpected behavior, including incomplete data and DTB failures.

## Targets that run during design-time builds

The following design-time targets are called, including any dependencies, during design-time builds in the C#/VB project systems. Other project systems, such as C++ or JavaScript will call different targets. 

Design-Time Target                             | Defined by | Description
-----------------------------------------------|------------|------------------
BuiltProjectOutputGroup                        | MSBuild    | 
CollectAnalyzersDesignTime                     | DNPS       | Returns `Analyzer` items.
CollectCentralPackageVersions                  | NuGet      | Returns `PackageVersion` items.
CollectCopyToOutputDirectoryItemDesignTime     | DNPS       | Identifies items the project contributes to the output directory during build. Supports the Fast Up-to-date Check and Build Acceleration.
CollectFrameworkReferences                     | NuGet      | Returns non-transitive `FrameworkReference` items. Supports package restore.
CollectNuGetAuditSuppressions                  | NuGet      | Returns `NuGetAuditSuppress` items. Supports package restore.
CollectPackageDownloads                        | NuGet      | Returns `PackageDownload` items. Supports package restore.
CollectPackageReferences                       | NuGet      | Returns `PackageReference` items. Supports package restore.
CollectPrunePackageReferences                  | NuGet      | Returns `PrunePackageReference` items. Supports package restore.
CollectResolvedCompilationReferencesDesignTime | DNPS       |
CollectResolvedSDKReferencesDesignTime         | SDK        |
CollectSuggestedVisualStudioComponentIds       | DNPS       | Supports in-product acquisition (IPA).
CollectUpToDateCheckBuiltDesignTime            | DNPS       | Supports the Fast Up-to-date Check.
CollectUpToDateCheckInputDesignTime            | DNPS       | Supports the Fast Up-to-date Check.
CollectUpToDateCheckOutputDesignTime           | DNPS       | Supports the Fast Up-to-date Check.
CompileDesignTime                              | MSBuild    | Passes command-line arguments, `<Compile>` items and `<Analyzer>` items to the compiler in normal builds, or to the language service in design-time builds.
GenerateSupportedTargetFrameworkAlias          | SDK        | Returns `SupportedTargetFrameworkAlias` items. Supports the Project Properties UI.
ResolveAssemblyReferencesDesignTime            | MSBuild    | Resolves `Reference` items to their paths. Supports the Dependencies Node.
ResolveComReferencesDesignTime                 | MSBuild    | Resolves `COMReference` items to their primary interop assemblies (PIA) paths. Supports the Dependencies Node.
ResolveFrameworkReferencesDesignTime           | MSBuild    | Resolves `FrameworkReference` items to their paths. Supports the Dependencies Node.
ResolvePackageDependenciesDesignTime           | MSBuild    | Resolves `PackageReference` items to their paths. Supports the Dependencies Node.
ResolveProjectReferencesDesignTime2            | DNPS       | Resolves `ProjectReference` items to their output paths. Overrides `ResolveProjectReferencesDesignTime` from MSBuild's common targets. Supports the Dependencies Node.

Where DNPS is the .NET Project System (this repository).
