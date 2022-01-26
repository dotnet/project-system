# Design-time builds

- [What is a design-time build?](#what-is-a-design-time-build)
- [Targets that run during design-time builds](#targets-that-run-during-design-time-builds)
- [Designing targets for use in design-time builds](#designing-targets-for-use-in-design-time-builds)
- [Diagnosing design-time builds](#diagnosing-design-time-builds)

## What is a design-time build?

Design-time builds are special builds that are launched by the project system to gather just enough information to populate the language service and other project services, such as the Dependencies node.  Design-time builds are not directly user-initiated, but may be indirectly launched in response to a user action such as changing the project file, build options, adding/removing source files and references, or switching configurations.

For performance reasons, and unlike normal builds which call the _Build_ target, design-time builds call a limited set of targets. This can lead to custom builds that succeed during a normal build, but end up failing during a design-time build, typically due to custom targets with under-specified dependencies.

## Targets that run during design-time builds

The following design-time targets are called, including any dependencies, during design-time builds in the C#/VB project systems. Other project systems, such as C++ or JavaScript will call different targets. 

Design-Time Target                            | Normal Target                      | Description
----------------------------------------------|------------------------------------|------------------
ResolveAssemblyReferencesDesignTime           | ResolveAssemblyReferences          | Resolves `<Reference>` items to their paths.
ResolveProjectReferencesDesignTime            | ResolveProjectReferences           | Resolves `<ProjectReference>` items to their output paths.
ResolveComReferencesDesignTime                | ResolveComReferences               | Resolves `<COMReference>` items to their primary interop assemblies (PIA) paths.
ResolveFrameworkReferencesDesignTime          | ResolveFrameworkReferences         | Resolves `<FrameworkReference>` items to their paths.
ResolvePackageDependenciesDesignTime          | ResolvePackageDependencies         | Resolves `<PackageReference>` items to their paths.
CompileDesignTime (new project system)/Compile| Compile                            | Passes command-line arguments, `<Compile>` items and `<Analyzer>` items to the compiler in normal builds, or to the language service in design-time builds.

The design-time targets are typically simple wrappers around their normal target equivalents, with customized behavior for design-time builds. 

## Designing targets for use in design-time builds

Targets that dynamically change references, source files or compilation options _must_ run during design-time builds to avoid unexpected behavior in Visual Studio. In contrast, if a target does not contribute these items, then it should actively avoid running in these builds to ensure design-time builds are as fast as possible. Whether a target is run in design-time builds is based on whether a target's `BeforeTargets` and `AfterTargets` attributes specifies a direct or indirect dependency of any of the above  targets. See [Diagnosing design-time builds](#diagnosing-design-time-builds) to see logs that help you figure out if your target is being run or not.

### Running in a design-time build

If you've determined that your target needs to run in a design-time build, using the above table set `BeforeTargets` to the normal target equivalent of what you are contributing to the build. For example, if a target changes `<Reference>` items, then it should indicate that it runs _before_ `ResolveAssemblyReferences` target:

``` XML
  <Target Name="AddAdditionalReferences" BeforeTargets="ResolveAssemblyReferences">
     ...
  </Target>
```
The `AddAdditionalReferences` target will run in both normal builds _and_ design-time builds, leading to consistent results between them.

### Determining whether a target is running in a design-time build

Use both the `DesignTimeBuild` (CPS-based projects) and `BuildingProject` (legacy project system) properties to determine whether a target is running in a design-time build or a normal build. This can be used to avoid expensive calculations or work that is only needed for a normal build, helping to keep the IDE responsive.

``` XML
  <Target Name="AddAdditionalReferences" BeforeTargets="ResolveAssemblyReferences">
     <PropertyGroup Condition="'$(DesignTimeBuild)' == 'true' OR '$(BuildingProject)' != 'true'">
         <_AvoidExpensiveCalculation>true</_AvoidExpensiveCalculation>
     </PropertyGroup>
     ...
  </Target>
```

__NOTE:__ The `DesignTimeBuild` property is typically empty (`''`) in normal builds, so avoid comparisons to `'false'`.
 
### Specifying explicit dependencies

If your target has dependencies on properties, items or files produced during the build, it must have a `DependsOnTargets` attribute that accurately indicates the set of targets that produce those assets. An under-specified `DependsOnTargets` will lead to unexpected behavior, such as targets that fail on the first design-time build or fail during every design-time build.

## Diagnosing design-time builds

### Signs that a design-time build is failing or taking too long

While the results of design-time builds are not directly visible by default, the following symptoms are good indicators that one is failing for a given project:

- Source files in a project are marked as coming from the `Miscellaneous Files` project when opened in the editor
- IntelliSense shows incomplete and/or incorrect results
- A normal build succeeds inside and outside of Visual Studio, yet the Error List continues to show build errors

The following are symptoms of a design-time build that is taking too long:

- Project modifications, such as renaming, adding or deleting files, take a long time
- Switching build configurations, for example from Debug to Release, takes a long time

### Getting Visual Studio to output the results of a design-time build

You can force Visual Studio to show the results of a design-time build using the following instructions:

#### Visual Studio 2015 or below

1. Delete the `.vs` directory that sits alongside the solution that is experiencing the problem
2. Start a _Developer Command Prompt for VS2015_
3. At the prompt, run `SET TRACEDESIGNTIME=true`
4. At the prompt, run `devenv`
5. Open the solution
6. Under `%TEMP%`, look for `[RANDOMGUID].designtime.log` files, these will contain the results of the design-time build. If running Visual Studio 2015 Update 2 or higher, the name of the project and design-time target that is being called will also be included in the file name.

#### Visual Studio 2017 or later

1. Install the [Project System Tools](https://github.com/dotnet/project-system-tools#project-system-tools) extension
2. In Visual Studio, choose the `View > Other Windows > Build Logging` menu item.
3. Click on the "play" button.

This will cause design-time builds to show up in the build logging tool window. If you have the [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) installed, you can double-click on a log to view it in the viewer, otherwise you can right-click and choose `Save As...` to save the log in the new [binary log format](https://github.com/Microsoft/msbuild/wiki/Binary-Log).

### Diagnosing failing or slow design-time builds

After following the above instructions, open the resulting build log file or Output window (for the new project system).

#### Failing design-time build
For a failing build, look for errors at the end of the log:

```
Build FAILED.

c:\Projects\MyProject\MyProject.csproj(17,5): error : An error occurred!
    0 Warning(s)
    1 Error(s)
```

These errors indicate that a target failed, typically this is due to targets that have not correctly specified their dependencies.


#### Slow design-time build
For a slow design-time, look for the target performance summary at end of the log which can indicate long running tasks and targets:

```
Target Performance Summary:
        0 ms  AfterClean                                 1 calls
        0 ms  Clean                                      1 calls
        0 ms  CleanReferencedProjects                    1 calls
        0 ms  CleanPublishFolder                         1 calls
        0 ms  BeforeRebuild                              1 calls
        0 ms  BeforeClean                                1 calls
        0 ms  BeforeBuild                                1 calls
        0 ms  _SplitProjectReferencesByFileExistence     1 calls
        1 ms  CleanXsdCodeGen                            1 calls
        2 ms  AssignProjectConfiguration                 1 calls
        7 ms  CoreClean                                  1 calls
       10 ms  _CheckForInvalidConfigurationAndPlatform   1 calls

Task Performance Summary:
        0 ms  RemoveDuplicates                           1 calls
        0 ms  Error                                      1 calls
        1 ms  MakeDir                                    1 calls
        1 ms  Message                                    2 calls
        1 ms  ReadLinesFromFile                          1 calls
        1 ms  WriteLinesToFile                           1 calls
        1 ms  FindUnderPath                              2 calls
        1 ms  AssignProjectConfiguration                 1 calls
        2 ms  Delete                                     3 calls
``` 
