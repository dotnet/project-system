# Up-to-date Check

The Project System's _Fast Up-to-date Check_ save developers time by quickly assessing whether a project needs to be
built or not. If not, Visual Studio can avoid a comparatively expensive call to MSBuild.

At a superficial level, the check compares timestamps between the project's inputs and its outputs. For more
information on how it works in detail, see [this document](repo/up-to-date-check-implementation.md).

## Customization

For most projects the up-to-date check works automatically and you won't need to know or think about this feature.
However if your build is highly customized then you may need to provide some extra information to help the up-to-date
check work correctly.

For customized builds, you may add to the following item types:

- `UpToDateCheckInput` &mdash; Describes an input file that MSBuild would not otherwise know about
- `UpToDateCheckOutput` &mdash; Describes an output file that MSBuild would not otherwise know about
- `UpToDateCheckBuilt` &mdash; Similar to `UpToDateCheckOutput` but with optional `Original` property that denotes
  that the output was copied, not built.

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
- `CollectUpToDateCheckOutputDesignTime`
- `CollectUpToDateCheckBuiltDesignTime`

Note that changes to inputs **must** result in changes to outputs. If this rule is not observed, then an input may
have a timestamp after all outputs, which leads the up-to-date check to consider the project out-of-date after building.

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

With a log level of info or verbose, you'll see messages prefixed with `FastUpToDate:` in Visual Studio's build output.
These messages will explain why the project is considered up-to-date or not.

## Disabling the Up-to-date Check

If you do not wish to use the fast up-to-date check, preferring to always call MSBuild, you can disable it by either:

- Unchecking "Don't call MSBuild if a project appears to be up to date" (shown above), or
- Adding property `<DisableFastUpToDateCheck>True</DisableFastUpToDateCheck>` to your project

Note that in both cases this only disables Visual Studio's up-to-date check. MSBuild will still perform its own
determination as to whether the project should be rebuilt.

If you are disabling the check because you feel it is not behaving correctly, please file an issue in this repo and
include details from the verbose log so that we can improve the feature.
