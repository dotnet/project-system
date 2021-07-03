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
- `UpToDateCheckBuilt` &mdash; Describes an output file that MSBuild would not otherwise know about

Note that `UpToDateCheckOutput` exists but is deprecated and only maintained for backwards compatability.
Projects should use to `UpToDateCheckBuilt` instead.

You may add to these item types declaratively. For example:

```xml
<ItemGroup>
  <UpToDateCheckInput Include="MyCustomBuildInput.abc" />
</ItemGroup>
```

Alternatively, you may override the MSBuild targets that Visual Studio calls to collect these items. Overriding targets
allows custom logic to be executed when determining the set of items. The relevant targets are defined in
`Microsoft.Managed.DesignTime.targets` with names:

- `CollectUpToDateCheckInputDesignTime`
- `CollectUpToDateCheckBuiltDesignTime`

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
<UpToDateCheckBuilt Include="Source\File.txt" Original="Destination\File.txt" />
```

When specifying `Original` metadata, the `Set` property has no effect. Each copied file is considered in isolation,
looking only at the timestamps of the source and destination. Sets are used to compare groups of items, so these
features do not compose. If both properties are present, `Original` will take effect and `Set` is ignored.

## Debugging

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

## Disabling the Up-to-date Check

If you do not wish to use the fast up-to-date check, preferring to always call MSBuild, you can disable it by either:

- Unchecking "Don't call MSBuild if a project appears to be up to date" (shown above), or
- Adding property `<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>` to your project

Note that in both cases this only disables Visual Studio's up-to-date check. MSBuild will still perform its own
determination as to whether the project should be rebuilt.

If you are disabling the check because you feel it is not behaving correctly, please file an issue in this repo and
include details from the verbose log so that we can improve the feature.
