# Design-time builds

- [What is a design-time build?](#what-is-a-design-time-build)
- [Targets that run during design-time builds](#targets-that-run-during-design-time-builds)
- [Designing targets for use in design-time builds](#designing-targets-for-use-in-design-time-builds)
- [Diagnosing design-time builds](#diagnosing-design-time-builds)

## What is a design-time build?

Design-time builds are special builds that are launched by the project system to gather just enough information to populate the language service and other project services, such as the references node.  Design-time builds are not directly user-initiated, but may be indirectly launched in response to a user action such as changing the project file or build options, or adding/removing source files and references, or switching configurations.

For performance reasons and unlike normal builds, which call the _Build_ target, design-time builds call a limited set of targets. This can lead to custom builds that succeed during a normal build, but end up failing during a design-time build, typically due to custom targets with under-specified dependencies.

## Targets that run during design-time builds

The following design-time targets are called, including any dependencies, during design-time builds in the C#/VB project systems. Other project systems, such as C++ or JavaScript will call different targets. 

Design-Time Target                            | Normal Target                      | Description
----------------------------------------------|------------------------------------|------------------
ResolveAssemblyReferencesDesignTime           | ResolveAssemblyReferences          | Resolves `<Reference>` items to their paths.
ResolveProjectReferencesDesignTime            | ResolveProjectReferences           | Resolves `<ProjectReference>` items to their output paths.
ResolveComReferencesDesignTime                | ResolveComReferences               | Resolves `<COMReference>` items to their primary interop assemblies (PIA) paths.
CompileDesignTime (new project system)/Compile| Compile                            | Passes command-line arguments, include `<Compile>` and `<Analyzer>` items to the compiler in normal builds, or language service in design-time builds.

The design-time targets are typically simple wrappers around their normal target equivalents, with customized behavior for design-time builds. 

## Designing targets for use in design-time builds

Targets that dynamically change references, source files or compilation options _must_ run during design-time builds to avoid unexpected behavior in Visual Studio. In contrast, if a target does not contribute these items, then it should actively avoid running in these builds to ensure design-time builds are as fast as possible. Whether a target is run in design-time builds is based on whether a target's `BeforeTargets` and `AfterTargets` attributes specifies a direct or indirect dependency of any of the above  targets. See [Diagnosing design-time builds](#diagnosing-design-time-builds) to see logs that help you figure out if your target is being run or not.

### Running in a design-time build

If you've determined that your target needs to run in a design-time build, using the above table set `BeforeTargets` to the normal target equivalent of what you are contributing to the build. For example, if a target changes `<Reference>` items, then it should indicate that it runs _before_ `ResolveAssemblyReferences` target:

``` XML
  <Target Name="AddAdditionalReferences" BeforeTargets="ResolveAssemblyReference">
     ...
  </Target>
```
The `AddAdditionalReferences` target will run in both normal builds _and_ design-time builds, leading to consistent results between them.

### Determining whether a target is run in a design-time build

Use the `DesignTimeBuild` property to differentiate between when a target is run in a design-time build versus a normal build. This can be used to avoid expensive calculations or work that is only needed for a normal build.

``` XML
  <Target Name="AddAdditionalReferences" BeforeTargets="ResolveAssemblyReference">
     <PropertyGroup Condition="'$(DesignTimeBuild)' == 'true'">
         <_AvoidExpensiveCalculation>true</_AvoidExpensiveCalculation>
     </PropertyGroup>

     ...
  </Target>
```
 
### Specifying explicit dependencies

If your target has dependencies on properties, items or files produced during the build, it must have an accurate `DependsOnTargets` attribute that indicates the set of targets that produce those assets. An under-specified `DependsOnTargets` will lead to unexpected behavior, such as targets that fail on the first design-time build or fail during every design-time build.

## Diagnosing design-time builds

### Signs that a design-time build is failing or taking too long

While the results of design-time builds are not directly visible by default, the following symptoms are good indicators that one is failing for a given project:

- Source files in a project are marked as coming from the `Miscellaneous Files` project when opened in the editor
- IntelliSense shows incomplete and/or incorrect results
- A normal build succeeds inside and outside of Visual Studio, yet the Error List continues to show build errors

The following are symptoms of a design-time build that is taking too long:

- Making modifications to project, such as renaming, adding or deleting files take a large amount of time
- Switching build configurations, for example, from Debug to Release, takes large amounts of time

### Getting Visual Studio to output the results of a design-time build

You can force Visual Studio to show the results of a design-time build using the following instructions:

#### Visual Studio 2015 or below:

1. Delete the `.vs` directory that sits alongside the solution that is experiencing the problem
2. Start a Developer Command Prompt for VS2015
3. At the prompt, run `SET TRACEDESIGNTIME=true && devenv`
4. Open the solution
5. Under %TEMP%, look for [RANDOMGUID].designtime.log files, these will contain the results of the design-time build. If running Visual Studio 2015 Update 2 or higher, the name of the project and design-time target that is being called will also be included in the file name.

#### Visual Studio "15"

In Visual Studio "15" there are two C# and Visual Basic project systems. By default, at the time of writing, the majority of projects continue to open in the same project system as previous versions of Visual Studio, and hence the instructions are the same as above.

If, however, you know that your project opens in the new [project system](http://github.com/dotnet/roslyn-project-system), you can use the following steps to see the results of design-time builds:

1. Under `HKEY_CURRENT_USER\SOFTWARE\Microsoft\VisualStudio\15.0\CPS`
create a new DWORD (32-bit) value `Design-time Build Logging` and set it to `1`. If running inside other hives of Visual Studio, replace `15.0` with `15.0[Hive]`, where `[Hive]` is the name of the hive.

2. Open the solution

The results of the design time build will appear in a new category called __Build - Design-time__ in the __Output__ window. The verbosity of the category respects the same settings under __Tools__ -> __Options__ -> __Project and Solutions__ -> __Build and Run__ as normal builds.

### Diagnosing why a design-time build is failing or taking too long

After following the above instructions, open the resulting build log file or Output window (for the new new project system).

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
For a slow design-time, look for the target performance summary at end of the long which can indicate long running tasks and targets:

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
