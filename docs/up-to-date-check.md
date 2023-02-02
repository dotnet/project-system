# Up-to-date Check

The Project System's _Fast Up-to-date Check_ saves developers time by quickly assessing whether a project needs to be
built or not. If not, Visual Studio can avoid a comparatively expensive call to MSBuild.

At a superficial level, the check compares timestamps between the project's inputs and its outputs. For more
information on how it works in detail, see [this document](repo/up-to-date-check-implementation.md).

Note that the _fast_ up-to-date check is intended to speed up the majority of cases where a build is not required,
yet it cannot reliably cover all cases correctly. Where necessary, it errs on the side of caution as triggering a
redundant build is better than not triggering a required build. MSBuild performs its own checks, so even if the 
fast up-to-date check incorrectly determines the project is out-of-date, MSBuild may still not perform a full
build.

## Customization

For most projects the up-to-date check works automatically and you won't need to know or think about this feature.
However if your build is highly customized then you may need to provide some extra information to help the up-to-date
check work correctly.

For customized builds, you may add to the following item types:

- `UpToDateCheckInput` &mdash; Describes an input file that MSBuild would not otherwise know about
- `UpToDateCheckOutput` &mdash; Describes an output file that MSBuild would not otherwise know about
- `UpToDateCheckBuilt` &mdash; Describes an output file that's produced from a single input file, that MSBuild would not otherwise know about

You may add to these item types declaratively. For example:

```xml
<ItemGroup>
  <UpToDateCheckInput Include="MyCustomBuildInput.abc" />
  <UpToDateCheckOutput Include="MyCustomBuildOutput.def" />
</ItemGroup>
```

Alternatively, you may override the MSBuild targets that Visual Studio calls to collect these items. Overriding targets
allows custom logic to be executed when determining the set of items. The relevant targets are defined in
`Microsoft.Managed.DesignTime.targets` with names:

- [`CollectUpToDateCheckInputDesignTime`](https://github.com/dotnet/project-system/blob/255712176d4b5dc4be054a45a5f63048aa89f4de/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/DesignTimeTargets/Microsoft.Managed.DesignTime.targets#L414-L415)
- [`CollectUpToDateCheckOutputDesignTime`](https://github.com/dotnet/project-system/blob/255712176d4b5dc4be054a45a5f63048aa89f4de/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/DesignTimeTargets/Microsoft.Managed.DesignTime.targets#L417-L418)
- [`CollectUpToDateCheckBuiltDesignTime`](https://github.com/dotnet/project-system/blob/255712176d4b5dc4be054a45a5f63048aa89f4de/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/DesignTimeTargets/Microsoft.Managed.DesignTime.targets#L420-L445)

Note that changes to inputs **must** result in changes to outputs. If this rule is not observed, then an input may
have a timestamp after all outputs, which leads the up-to-date check to consider the project out-of-date after building
indefinitely. This can lead to longer build times.

### Grouping inputs and outputs into sets

For some advanced scenarios, it's necessary to partition inputs and outputs into groups and consider each separately.
This can be achieved by adding `Set` metadata to the relevant items.

For example, an ASP.NET project may use sets to group Razor `.cshtml` files with their output assembly `MyProject.Views.dll`,
which is distinct from the other compilation target `MyProject.dll`. This could be achieved with something like:

```xml
<ItemGroup>
  <UpToDateCheckInput Include="Home.cshtml" Set="Views" />
  <UpToDateCheckOutput Include="MyProject.Views.dll" Set="Views" />
</ItemGroup>
```

Items that do not specify a `Set` are included in the default set. Items may be added to multiple sets by separating
their names with a semicolon (e.g. `Set="Set1;Set2"`).

### Filtering inputs and outputs

It may be desirable for a component within Visual Studio to schedule a build for which only a subset of the up-to-date
check inputs and outputs should be considered. This can be achieved by adding `Kind` metadata to the relevant items and
passing the `FastUpToDateCheckIgnoresKinds` global property.

For example:

```xml
<ItemGroup>
  <UpToDateCheckInput Include="Source1.cs" Kind="Alpha" />
  <UpToDateCheckInput Include="Source2.cs" />
  <UpToDateCheckOutput Include="MyProject1.dll" Kind="Alpha" />
  <UpToDateCheckOutput Include="MyProject2.dll" />
</ItemGroup>

<PropertyGroup>
  <FastUpToDateCheckIgnoresKinds>Alpha</FastUpToDateCheckIgnoresKinds>
<PropertyGroup>
```

If the `FastUpToDateCheckIgnoresKinds` property has a value of `Alpha`, then the fast up-to-date check will only
consider `Source2.cs` and `MyProject2.dll`. If the `FastUpToDateCheckIgnoresKinds` property has a different
value, or is empty, all four items are considered.

Multiple values may be passed for `FastUpToDateCheckIgnoresKinds`, separated by semicolons (`;`).

### Copied files

Builds may copy files from a source location to a destination location. Information about these locations should be
captured in the project so that the up-to-date check can determine if the source file is newer than the destination,
in which case the project is out-of-date and a build will be allowed.

To model this, use:

```xml
<UpToDateCheckBuilt Include="Destination\File.txt" Original="Source\File.txt" />
```

When specifying `Original` metadata, the `Set` property has no effect. Each copied file is considered in isolation,
looking only at the timestamps of the source and destination. Sets are used to compare groups of items, so these
features do not compose. If both `Set` and `Original` metadata are present, `Original` will take effect and `Set` is ignored.

### Transformed files

Cases where a single input file produces a single output file during build should be modelled in the same way as copied files above. The fast up-to-date check only inspects the timestamps of copied files, not their contents.

To model this, use:

```xml
<UpToDateCheckBuilt Include="Destination\MyFile.js" Original="Source\MyFile.ts" />
```

The same details apply regarding `Set` metadata as described for copied files.

When multiple inputs produce one or more outputs, use `BuildUpToDateCheckInput` and `BuildUpToDateCheckOutput` items with `Set` metadata, as described earlier in this document.

---

## Debugging

### SDK-Style projects

By default the up-to-date check does not log anything, though you can infer its decision from your build output summary:

```text
========== Build: 0 succeeded, 0 failed, 1 up-to-date, 0 skipped ==========
```

In this example the up-to-date check determined the project was up-to-date. If `succeeded` or `failed` was instead
non-zero, then the check would have determined the project was not up-to-date, resulting in a call to MSBuild.

To debug issues with the up-to-date check, enable its logging.

> Tools | Options | Projects and Solutions | SDK-Style Projects

![Projects and Solutions, .NET Core options](repo/images/options.png)

Setting this level to a value other than `None` results in messages prefixed with `FastUpToDate:` in Visual Studio's
build output.

- `None` disables log output.
- `Minimal` produces a single message per out-of-date project.
- `Info` and `Verbose` provide increasingly detailed information about the inner workings of the check, which are useful for debugging.

### .NET Framework (non-SDK-style) projects

There is no built-in way to enable up-to-date check logging for old-style (non-SDK) projects. The [Tweakster extension](https://github.com/madskristensen/Tweakster#up-to-date-check-verbose) provides a UI option for this, however.

Alternatively, to enable logging manually:

1. Open a "Developer Command Prompt" for the particular version of Visual Studio you are using.
2. Enter command:
   ```text
   vsregedit set "%cd%" HKCU General U2DCheckVerbosity dword 1
   ```
3. The message `Set value for U2DCheckVerbosity` should be displayed

Run the same command with a `0` instead of a `1` to disable this logging.

Note that `"%cd%"` evaluates to the current directory. When you first open a Developer Prompt, this path will be correct. To execute this command from arbitrary locations, you'll need to substitute the relevant quoted path, such as `"C:\Program Files\Microsoft Visual Studio\2022\Enterprise"`, with no trailing `/` or `>` character.

You can change this value while VS is running and it will take effect immediately.

When logging is enabled you'll see messages such as this in build output:

> Project 'MyProject' is not up to date. Input file 'c:\path\myproject\class1.cs' is modified after output file 'C:\Path\MyProject\bin\Debug\MyProject.pdb'.

#### Logging from the experimental hive

If you wish to enable this logging for a particular hive (this is an advanced scenario) then pass the hive's name after the path. For example:

```text
vsregedit set "%cd%" Exp HKCU General U2DCheckVerbosity dword 1
```

### Binary logs

The fast up-to-date check logging will explain the reason for the failure at a high level. Often it's necessary to dig
deeper into the build to understand why the failure occurs.

The best technique for this is to:

1. Capture a binary build log (also called a "binlog"), and
1. view it with the [MSBuild structured log viewer](https://msbuildlog.com/).

The [Project System Tools](https://github.com/dotnet/project-system-tools) extension enables capturing binlogs for builds that happen within Visual Studio.

The logs captured by that tool are usually adequate to diagnose build problems. They exclude some detail however, for performance reasons. If more data is required, see [this technique to get full-fidelity logs](https://github.com/dotnet/project-system-tools#getting-higher-fidelity-logs-from-vs).

---

## CopyToOutputDirectory Always vs. PreserveNewest

To copy items to the output directory during build, you have two options:

1. `CopyToOutputDirectory="Always"` always copies source over destination.
2. `CopyToOutputDirectory="PreserveNewest"` copies the file if the timestamp of the source is newer than the destination.

Many users select `Always` when they really would be happy with `PreserveNewest`. Historically, having an `Always` item caused the fast up-to-date check to immediately schedule a build, even if the files were unchanged. This meant that accidentally selecting `Always` would break incremental build performance. This is a very common performance issue in real world projects.

There is one valid use case for `Always`, which is to restore a data file in the output directory back to some initial state, in cases where the executable modifies that data file. If on the next launch the file should be restored to its previous state, then `Always` is the correct option. If not, you want `PreserveNewest`.

In VS 17.2, we changed the fast up-to-date check to be less strict about `Always` items. With this new behaviour, the check verifies the size and timestamps of the source and destination files. If they differ a build will be scheduled. This change can have a massively positive impact on inner loop productivity, when `Always` items exist.

## Reference assemblies and mixed SDK-style/non-SDK-style projects

[Reference assemblies](https://docs.microsoft.com/dotnet/standard/assembly/reference-assemblies) can be used during .NET builds to avoid unnecessary compilation in the following case:

1. A _referencing_ project has a `ProjectReference` to a _referenced_ project that specifies `ProduceReferenceAssembly` as `true`.
2. The _referenced_ project is changed in a way that doesn't alter its public API (such as a change within a method body, or the addition of a `private` method).
3. A build is requested for the _referencing_ project.
4. The _referenced_ project is built first, and that build notices that the public API hasn't changed.
5. The _referencing_ project is build second, and identifies that the referenced public API hasn't changed, so the build needs only to copy the referenced binary, without recompiling itself.

In a large solution with many layers of project references, this feature can avoid many recompilation chain reactions, reducing build times significantly.

Prior to VS 17.5, reference assemblies were not supported for non-SDK-style projects. In a solution that mixes both SDK-style and non-SDK-style projects, the interaction between the two projects in steps 4 and 5 above won't happen correctly. What happens instead is that the _referencing_ non-SDK-style project thinks the _referenced_ SDK-style project is up-to-date, so no build is scheduled. This means that the _referenced_ binary is not copied to the _referencing_ project's output directory. From the perspective of the developer, their changes appear to have no effect when running their program.

If you are experiencing this issue, you may either:

1. Upgrade to Visual Studio 17.5 or later (ensuring all developers of your solution also upgrade), or
2. Convert all non-SDK-style projects to SDK-style, or
3. Set `CompileUsingReferenceAssemblies` to `false` for all non-SDK-style projects. Note that you cannot use a `Directory.Build.props` file to achieve this if any SDK-style projects would also pick up that file. The property would need to be conditional, and non-SDK-style projects do not support conditions in these files ([see](https://github.com/dotnet/project-system/issues/4175)).

## Disabling the up-to-date check

If you do not wish to use the fast up-to-date check, preferring to always call MSBuild, you can disable it by either:

- unchecking "Don't call MSBuild if a project appears to be up to date" (shown above), or
- adding property `<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>` to your project.

Note that in both cases this only disables Visual Studio's up-to-date check. MSBuild will still perform its own
determination as to whether the project should be rebuilt.

> ⚠️ We do not recommend disabling this! It can have a significant negative impact on your productivity.
> If you are disabling the check because you feel it is not behaving correctly, please file an issue in this repo and
> include details from the verbose log so that we can improve the feature.

## Reasons a project is not up-to-date

There are several reasons that a project may fail its up-to-date check and be scheduled to build.

| Reason | Description |
|--------|-------------|
| *InputNewerThanEarliestOutput* | A project input (such as a `.cs` file) has a time stamp later than the earliest output time stamp. |
| *InputModifiedSinceLastSuccessfulBuildStart* | A project input (such as a `.cs` file) has a time stamp that comes after the last successful build time. This case would usually be covered by _InputNewerThanEarliestOutput_ as builds usually update output files, however there are cases where outputs are not updated, and this check ensures we don't end up in an overbuild loop, as such loops have significant penalties on inner-loop productivity. |
| *FirstRun* | This is the first build of the project. The FUTD check doesn't have persisted data in the `.vs` folder, so doesn't know the previous set of inputs that contributed to the current outputs (i.e. we could miss a case of reason *ProjectItemsChangedSinceLastSuccessfulBuildStart*). We schedule a build just to be sure. Once that build completes, this reason will not resurface unless the `.vs` folder is deleted. |
| *InputNotFound* | A project input (such as a `.cs` file) is not found on disk. A build is scheduled that may either produce this file and copy it, or emit an error about the missing file. |
| *OutputNotFound* | A project output (such as a `.dll` file) is not found on disk. A build is scheduled that will likely produce it. |
| *InputMarkerNewerThanOutputMarker* | This reason is seen when using [reference assemblies](https://docs.microsoft.com/dotnet/standard/assembly/reference-assemblies) and `ProjectReference`. It indicates that the referenced project's implementation assembly was changed, despite its reference assembly being unchanged. This situation occurs when the referenced project changes such that its public API is unmodified. The referencing project may not need to be recompiled, but must copy the updated implementation assembly to its output directory. |
| *ProjectItemsChangedSinceLastSuccessfulBuildStart* | The set of project items has changed since the time at which the last successful build started. This is required to correctly handle the removal of an input file that is included via globs. Without this check, removing a `.cs` file would not trigger a build, as no other observable change is present on disk. |
| *CopyDestinationNotFound* | A file is marked for copy, and the destination does not exist. The build will copy it. |
| *CopySourceNotFound* | A file is marked for copy, and the source does not exist. A build is scheduled that may either produce this file and copy it, or emit an error about the missing file. |
| *CopySourceNewer* | A file is marked for copy, and the source file was modified after the destination file. The build will update the destination file. |
| *CopyAlwaysItemDiffers* | A `CopyToOutputDirectory="Always"` item in the project has different source/target files (either by time stamp or file size). The build will copy it. |
| *CopyToOutputDirectoryDestinationNotFound* | A `CopyToOutputDirectory` input file is not present in the output directory. The build will copy it. |
| *CopyToOutputDirectorySourceNotFound* | A `CopyToOutputDirectory` input file does not exist. A build is scheduled that may either produce this file and copy it, or emit an error about the missing file. |
| *CopyToOutputDirectorySourceNewer* | A `CopyToOutputDirectory` input file has a newer time stamp than its destination. The build will update the output file. |
| *Disabled* | The project has `DisableFastUpToDateCheck` set. [More info](#disabling-the-up-to-date-check). |
| *CriticalTasks* | Critical build tasks are running. This is very uncommon, and is not something the user has control over. |
| *Exception* | An exception occurred in the implementation of the fast up-to-date check. We schedule a build at this point, as we don't know whether it is safe not to. |

## Discrepancies between FUTDC logging and build summary

If you enable [fast up-to-date check logging](#sdk-style-projects) (especially at verbose level) you may notice that sometimes a project is identified as up-to-date in those logs, yet the VS build summary says the project was built.

Here's an example of how that might appear:

```
...
1>FastUpToDate: Project is up-to-date. (MyProject)
1>FastUpToDate: Up-to-date check completed in 110.5 ms (MyProject)
1>------ Build started: Project: MyProject, Configuration: Debug Any CPU ------
========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
```

This log contradicts itself. It says the project was up-to-date, however it then goes on to report "build started" and finally "1 succeeded".

We would expect this instead:

```
...
1>FastUpToDate: Project is up-to-date. (MyProject)
1>FastUpToDate: Up-to-date check completed in 110.5 ms (MyProject)
========== Build: 0 succeeded, 0 failed, 1 up-to-date, 0 skipped ==========
```

The reason for this is as follows:

- When building, the Solution Build Manager (SBM) requests the fast up-to-date check (FUTDC) to run. For SDK-style .NET projects, this request is brokered by the Common Project System (CPS).
- The SBM is a very old component and makes a blocking (non-async) request for this information on the UI thread.
- The interaction between CPS and the FUTDC is non-blocking (async) and does not require the UI thread.
- While the FUTDC is running, VS is unresponsive as its UI thread is blocked waiting for the result.
- To prevent such a UI delay, if the FUTDC takes longer than some amount of time, CPS lies to the SBM, telling it the project is not up-to-date and must be built. The FUTDC operation continues however.
- Later, when the SBM calls back into CPS for the actual build to occur, the result of the FUTDC is taken into account. If the project actually _was_ up-to-date, then no build occurs. However the SBM doesn't know that, and just sees a very fast build.

Enabling verbose FUTDC logging is a good way to make the FUTDC take longer than the time CPS gives it.

This situation does not impact build correctness. It is a quirk that was intentionally introduced to keep VS responsive. It only applies to projects built via CPS, such as SDK-style .NET projects.

In future we hope to make the SBM async-aware, so that we can unblock the UI thread during these operations and report a correct build summary in all cases.