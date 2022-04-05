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

> Tools | Options | Projects and Solutions | .NET Core

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

To opt-out of this optimization and preserve the pre-17.2 behaviour, set the `DisableFastUpToDateCopyAlwaysOptimization` MSBuild property to `true` in your project. Consider also opening an issue on this repo to explain why the optimization doesn't work for you, to give us a chance to improve it further.

## Disabling the Up-to-date Check

If you do not wish to use the fast up-to-date check, preferring to always call MSBuild, you can disable it by either:

- unchecking "Don't call MSBuild if a project appears to be up to date" (shown above), or
- adding property `<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>` to your project.

Note that in both cases this only disables Visual Studio's up-to-date check. MSBuild will still perform its own
determination as to whether the project should be rebuilt.

⚠️ We do not recommend disabling this! It can have a significant negative impact on your productivity.
If you are disabling the check because you feel it is not behaving correctly, please file an issue in this repo and
include details from the verbose log so that we can improve the feature.
