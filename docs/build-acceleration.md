# Build acceleration

Build acceleration is a feature of Visual Studio that reduces the time required to build projects.

The feature was added in 17.5 and is currently opt-in. It applies to SDK-style .NET projects only. It is simple to try, and in most cases will improve build times. Larger solutions will see greater gains.

This document will outline what the feature does, how to enable it, and when it might not be suitable.

## Background

Visual Studio uses MSBuild to build .NET projects. There is some overhead associated with calling MSBuild to build each project, so Visual Studio uses a "fast up-to-date check" (FUTDC) to avoid calling MSBuild unless needed. This FUTDC can quickly determine if anything has changed in the project that would cause a build to be required. For more information, see [Up-to-date Check](up-to-date-check.md).

In several cases, the FUTDC identifies that no compilation is required, yet identifies some files need to be copied to the output directory, either from the current project or from a referenced one. Historically in this scenario, the FUTDC would call MSBuild to build the project, even though no compilation was required. This was done to ensure that the files were copied to the output directory.

With the build acceleration feature, Visual Studio will perform these files copies directly rather than calling MSBuild to do them.

## Example

Build acceleration is particularly impactful when changes need to be copied around several times during build.

```mermaid
graph LR
    A[Unit Test] --> B[Library 1]
    B --> C[Library 2]
    C --> D[Library 3]
```

Consider this example, where a unit test project references a project that in turn references another, and so on.

Making a change in _Library 3_ and running the unit test would previously have caused four calls to MSBuild.

With build acceleration enabled MSBuild is called only once, after which VS copies the output of _Library 3_ to all referencing projects

## Configuration

Build acceleration is currently opt-in.

To enable it in your solution, add or edit a top-level [`Directory.Build.props`](https://learn.microsoft.com/visualstudio/msbuild/customize-your-build) file to include:

```xml
<Project>
  <PropertyGroup>
    <AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>
  </PropertyGroup>
</Project>
```

You may disable build acceleration for specific projects in your solution by redefining the `AccelerateBuildsInVisualStudio` property as `false` in those projects.

## Debugging

Build acceleration runs with the FUTDC, and outputs details of its operation in the build log. To enable this logging:

> Tools | Options | Projects and Solutions | .NET Core

![Projects and Solutions, .NET Core options](repo/images/options.png)

Setting _Logging Level_ to a value other than `None` results in messages prefixed with `FastUpToDate:` in Visual Studio's build output.

- `None` disables log output.
- `Minimal` produces a single message per out-of-date project.
- `Info` and `Verbose` provide increasingly detailed information about the inner workings of the check, which are useful for debugging.

## Limitations

MSBuild is very configurable, and there are many ways to configure a project that will prevent build acceleration from working correctly. For example, if a project's build defines post-compile steps that are important to the correct functioning of your project, then build acceleration will not correctly reproduce those steps when it bypasses MSBuild.

Note that NuGet packages can modify a project's build in non-obvious ways that may have undesirable interactions with build acceleration.

We recommend enabling build acceleration for all projects in the solution, as described above, then monitoring for any unexpected behavior. You can use the log output to verify whether build acceleration is the culprit. If so, disable it for that project.

## Giving feedback

If you encounter an issue with build acceleration, please [file an issue](https://github.com/dotnet/project-system/issues/new/choose) and we will investigate whether it's something that can be addressed.
