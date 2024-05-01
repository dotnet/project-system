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

With build acceleration enabled MSBuild is called only once, after which VS copies the output of _Library 3_ to all referencing projects.

## Configuration

Build acceleration is currently opt-in.

To enable it in your solution, add or edit a top-level [`Directory.Build.props`](https://learn.microsoft.com/visualstudio/msbuild/customize-your-build) file to include:

```xml
<Project>
  <!--
    This Directory.Build.props files sets default properties that apply to all projects found in
    this folder or subfolders, recursively.
  -->
  <PropertyGroup>
    <!-- Enable Build Acceleration in Visual Studio. -->
    <AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>

    <!--
      If you target a framework earlier than .NET 5 (including .NET Framework and .NET Standard),
      you should set ProduceReferenceAssembly to true in order to speed incremental builds.
      If you multi-target and any target is before .NET 5, you need this.
      Even if you target .NET 5 or later, having this property is fine.
    -->
    <ProduceReferenceAssembly>true</ProduceReferenceAssembly>
  </PropertyGroup>
</Project>
```

You may disable build acceleration for specific projects in your solution by redefining the `AccelerateBuildsInVisualStudio` property as `false` in those projects.

### Disabling Build Acceleration for projects referencing specific NuGet packages

While rare, it's possible for a NuGet package to include `.props` and/or `.targets` files that customise the build in a way that's not compatible with Build Acceleration. Ideally such packages would also set `AccelerateBuildsInVisualStudio` to `false`, however that's not always an option.

To address this situation, you can specify the names of NuGet packages that, when present in a project, will disable Build Acceleration.

For example, if `MyPackage` is known to be incompatible with Build Acceleration, adding a `BuildAccelerationIncompatiblePackage` item  to your `Directory.Build.props` will automatically cause Build Acceleration to be disabled for any project that references that `MyPackage`:

```xml
<Project>
  <PropertyGroup>
    <AccelerateBuildsInVisualStudio>true</AccelerateBuildsInVisualStudio>
    <ProduceReferenceAssembly>true</ProduceReferenceAssembly>
  </PropertyGroup>
  <ItemGroup>
    <!-- Disable Build Acceleration for projects that reference specific incompatible packages. -->
    <BuildAccelerationIncompatiblePackage Include="MyPackage" />
  </ItemGroup>
</Project>
```

The .NET Project System includes a list of known, commonly used packages that don't work with Build Acceleration. To disable this default list, set the `EnableDefaultBuildAccelerationIncompatiblePackages` property to `false`.

## Debugging

### Enable logging

Build acceleration runs with the FUTDC, and outputs details of its operation in the build log. To enable this logging:

> Tools | Options | Projects and Solutions | SDK-Style Projects

_Traditional view:_

<img src="repo/images/options.png" width="528" alt="SDK-style project options, in the legacy settings view">

_Unified settings view:_

<img src="repo/images/options-unified.png" width="528" alt="SDK-style project options, in the modern unified settings view">

Setting _Logging Level_ to a value other than `None` results in messages prefixed with `FastUpToDate:` in Visual Studio's build output.

- `None` disables log output.
- `Minimal` produces a single message per out-of-date project.
- `Info` and `Verbose` provide increasingly detailed information about the inner workings of the check, which are useful for debugging.

### Validate builds are accelerated

If build acceleration cannot be enabled for any of the reasons given below, builds continue to work as before.

The following prerequisites exist for build acceleration:

- You are running Visual Studio 2022 version 17.5 or later.
- The project is an SDK-style .NET project.
- All projects it references (directly and transitively) are also SDK-style .NET projects.

The following steps will validate that build acceleration is working correctly for a given project.

1. Ensure `Verbose` logging is enabled (see [Enable logging](#enable-logging)).
1. Build the project to make it up-to-date.
1. Modify source of a referenced project (either a direct reference or transitive reference).
1. Build the project again.

Looking through the build output with the following points in mind:

- ℹ️ If you see:

   > This project appears to be a candidate for build acceleration. To opt in, set the 'AccelerateBuildsInVisualStudio' MSBuild property to 'true'.

   Then the project does not specify the `AccelerateBuildsInVisualStudio` property, or its value was not `true` or `false`, and the project would likely benefit from build acceleration. If the project cannot use build acceleration for any reason, this message can be suppressed by setting the property to `false` explicitly. See [configuration](#configuration) to learn how to configure build acceleration correctly.

- ⛔ If you see:

   > Build acceleration is disabled for this project via the 'AccelerateBuildsInVisualStudio' MSBuild property.

   Then the `AccelerateBuildsInVisualStudio` property was set to `false`. Even if your build files don't set this explicitly, it could come from a `.props`/`.targets` file within a NuGet package, or be related to the project type (for example, installer projects cannot be accelerated).

- ⛔ If you see:

   > Build acceleration data is unavailable for project with target 'C:\Solution\Project\bin\Debug\Project.dll'.

   Then any project that references the indicated project (directly or transitively) cannot be accelerated. This can happen if the mentioned project uses the legacy `.csproj` format, or for any other project system within Visual Studio that doesn't support build acceleration. Currently only .NET SDK-style projects (loaded with the project system from this GitHub repository) provide the needed data.

- ⛔ If you see:

   > Build acceleration is not available for this project because it copies duplicate files to the output directory: '<path1>', '<path2>'

   Then multiple projects want to copy the same file to the output directory. Currently, Build Acceleration does not attempt to discover which of these source files should win. Instead, when this situation occurs, Build Acceleration is disabled.

- ⛔ If you see:

   > This project has enabled build acceleration, but not all referenced projects produce a reference assembly. Ensure projects producing the following outputs have the 'ProduceReferenceAssembly' MSBuild property set to 'true': '&lt;path1&gt;', '&lt;path2&gt;'.

   Then build acceleration will not know whether it is safe to copy a modified output DLL from a referenced project or not. We rely on the use of reference assemblies to convey this information. To address this, ensure all referenced projects have the `ProduceReferenceAssembly` property set to `true`. You may like to add this to your `Directory.Build.props` file alongside the `AccelerateBuildsInVisualStudio` property. Note that projects targeting `net5.0` or later produce reference assemblies by default. Projects that target .NET Standard may require this to be specified manually (see https://github.com/dotnet/project-system/issues/8865).

   This message lists the referenced projects that are not producing a reference assembly. The `TargetPath` of those projects is used, as this can help disambiguate between target frameworks in multi-targeting projects.

   For more information, see [Reference Assemblies](#reference-assemblies).

- ✅ You should see a section listing items to copy:

   ```
   Checking items to copy to the output directory:
       Checking copy items from project 'C:\Solution\Referenced\Referenced.csproj':
           Checking PreserveNewest item
               Source      2023-01-19 15:28:56.882: 'C:\Solution\Referenced\bin\Debug\net7.0\Referenced.dll'
               Destination 2023-01-19 15:28:37.379: 'C:\Solution\Referencing\bin\Debug\net7.0\Referenced.dll'
               Remembering the need to copy file 'C:\Solution\Referenced\bin\Debug\net7.0\Referenced.dll' to 'C:\Solution\Referencing\bin\Debug\net7.0\Referenced.dll'.
       ...
   ```

   This indicates that build acceleration has identified a set of files to copy.

- ✅ You should see a section indicating that files were copied and the project was up-to-date:

   ```
   Copying 2 files to accelerate build:
       From 'C:\Solution\Library1\bin\Debug\net7.0\Library1.dll' to 'C:\Solution\Tests\bin\Debug\net7.0\Library1.dll'.
       From 'C:\Solution\Library1\bin\Debug\net7.0\Library1.pdb' to 'C:\Solution\Tests\bin\Debug\net7.0\Library1.pdb'.
   Project is up-to-date.
   Up-to-date check completed in 8.8 ms
   ```
   
   This indicates that rather than calling MSBuild to build the project, Visual Studio has copied the listed files directly. The check completed quickly, the project is reported as up-to-date, and the next project (if any) can start building.

   ⚠️ Note that the bug described in [Discrepancies between FUTDC logging and build summary](up-to-date-check.md#discrepancies-between-futdc-logging-and-build-summary) may cause the number of succeeded projects to be overstated. This requires changes within Visual Studio, and we hope to fix this in a future release.

## Reference Assemblies

A reference assembly is a DLLs that models the public API of a project, without any actual implementation.

During build, reference assembly timestamps are only updated when their project's public API changes. Incremental build systems use file system timestamps for many of their optimisations. If project changes are internal-only (e.g. method bodies, private members added/removed, documentation changed) then the timestamp is not changed. Knowing the time at which a public API was last changed allows skipping some compilation.

This is useful in multi-project builds, where projects reference one another. Consider two projects, where `A` references `B`:

```mermaid
graph LR
    A --> B
```

In our example, `A` only need to recompile if it has its own changes, or if `B`'s reference assembly changes. In the common case that `B`'s implementation changed but not its reference assembly, the build of `A` can be made faster by skipping recompilation and copying `B`'s implementation assembly into `A`'s output folder.

Production of a reference assembly is controlled by the `ProduceReferenceAssembly` MSBuild property, and the feature is part of MSBuild directly. This means it works well outside of VS, in case you also do CLI builds. Note that most CI builds are non-incremental (they happen on fresh clones), so this property has no impact there.

When `ProduceReferenceAssembly` was introduced in .NET 5, it was only enabled by default for .NET 5 and later. We investigated changing the default for earlier frameworks too, but this caused issues in a very small number of highly customised builds and we take backwards compatability very seriously. That said, it's generally desirable to configure projects to produce reference assemblies, regardless of whether you use Build Acceleration or not.

For more information, see [Reference Assemblies](https://learn.microsoft.com/dotnet/standard/assembly/reference-assemblies) on Microsoft Learn.

## Limitations

MSBuild is very configurable, and there are ways to configure a project that will prevent build acceleration from working correctly. For example, if a project's build has post-compile steps that are important to the correct functioning of your project, then build acceleration will not correctly reproduce those steps when it bypasses MSBuild.

We recommend enabling build acceleration for all projects in the solution, as described above, then monitoring for any unexpected behavior. You can use the log output to verify whether build acceleration is the culprit. If so, disable it for that project.

Some examples of project types for which build acceleration may not work correctly:

- **Installer projects** &mdash; builds must package files into some output file (`.exe`, `.msi`, `.vsix`, ...).
- **MAUI projects** &mdash; builds must produce a device-specific artifact for deployment.

Even if your solution has such a project, you should still enable build acceleration for all the other projects. Most large solutions have only one or two top-level projects like this, and many library projects that are candidates for acceleration.

Note that NuGet packages can modify a project's build in non-obvious ways that may have undesirable interactions with build acceleration. Theoretically a class library project having a specific NuGet package might not work with build acceleration. We are not aware of any such popular packages at this time.

We aim to automatically identify and disable build acceleration in cases where it won't work. Please let us know of such cases in an issue or discussion on this repo so that we can improve the feature and this documentation.

## Giving feedback

If you encounter an issue with build acceleration, please review the [open issue list](https://github.com/dotnet/project-system/issues?q=is%3Aissue+label%3AFeature-Build-Acceleration) to see whether it already exists. If so you can vote and comment on that issue. Otherwise, [file a new issue](https://github.com/dotnet/project-system/issues/new/choose) and we will investigate whether it's something that can be addressed.
