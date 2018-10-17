# Up-to-date Check

If a project's outputs are up-to-date with its inputs, there's no need to run a build.

The _Fast Up To Date Check_ attempts to perform a fast validation of whether a build is required.

The `IBuildUpToDateCheckProvider` interface (from CPS) has two members:

- `IsUpToDateCheckEnabledAsync`, which is serviced by `IProjectSystemOptions.GetFastUpToDateLoggingLevelAsync`
- `IsUpToDateAsync` which performs the checks described below

## What is checked

There are several checks which must all pass. 

All :heavy_check_mark: statements must be true for everything to be up to date.

Checks occur in the order listed.

As soon as one returns `false`, we are _not_ up to date and return immediately without performing further checks.

### Environment

- :heavy_check_mark: The query `BuildAction` is `Build`
- :heavy_check_mark: `IProjectAsynchronousTasksService.IsTaskQueueEmpty(ProjectCriticalOperation.Build)` is `true`
- :heavy_check_mark: We have received project and item data via Dataflow
- :heavy_check_mark: The current project's version is up to date with data received via Dataflow
- :heavy_check_mark: The list of items received via Dataflow has _not_ changed since the last check
- :heavy_check_mark: `DisableFastUpToDateCheck` is `false` or not specified
- :heavy_check_mark: No project items have `CopyToOutputDirectory` as `Always`

### Outputs

Output files break down as follows:

`_customOutputs` are `UpToDateCheckOutput` items published via `ProjectSubscription.JointRuleSource`
`_builtOutputs` are `UpToDateCheckBuilt` items published via `ProjectSubscription.JointRuleSource` (having no or empty `Original` property)

- :heavy_check_mark: All `_customOutputs` and `_builtOutputs` files exist

### Inputs

- :heavy_check_mark: All input files exist
- :heavy_check_mark: All input files are older than the earliest output (`_customOutputs` and `_builtOutputs`)
- :heavy_check_mark: All input files were modified before the last up-to-date check was performed

### `CopyUpToDateMarker` and `ResolvedCompilationReference`

If `ProjectSubscription.JointRuleSource` published a `CopyUpToDateMarker`...

- :heavy_check_mark: ...at least one `ResolvedCompilationReference` was also published
- :heavy_check_mark: ...at least one `ResolvedCompilationReference` file exists
- :heavy_check_mark: ...the `CopyUpToDateMarker` file exists
- :heavy_check_mark: ...the `CopyUpToDateMarker` file is newer than all existing `ResolvedCompilationReference` files

### Project items with `CopyToOutputDirectory` as `PreserveNewest`

All project items having `CopyToOutputDirectory` as `PreserveNewest`...

- :heavy_check_mark: ...exist
- :heavy_check_mark: ...have existing output files
- :heavy_check_mark: ...are older or equal to their output files

### Copied output files

`_copiedOutputFiles` is a map from destination to source paths (relative). It is populated with `UpToDateCheckBuilt` items published via `ProjectSubscription.JointRuleSource` having non-empty `Original` property.

For each `_copiedOutputFiles` source/destination

- :heavy_check_mark: ...the source must exist
- :heavy_check_mark: ...the destination must exist
- :heavy_check_mark: ...the destination must be older or the same age as the source

## Implementation

- [`BuildUpToDateCheck.cs`](https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/UpToDate/BuildUpToDateCheck.cs)
- [`BuildUpToDateCheckTests.cs`](https://github.com/dotnet/project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests/ProjectSystem/UpToDate/BuildUpToDateCheckTests.cs)

## Debugging

The `BuildUpToDateCheck` class uses a `BuildUpToDateCheckLogger` to log verbose and info level information that is very useful during debugging.

The log level is obtained via `IProjectSystemOptions.GetFastUpToDateLoggingLevelAsync` which is user-controlled in Visual Studio via:

> Tools | Options | Projects and Solutions | .NET Core

![Projects and Solutions, .NET Core options](images/options.png)

With a log level of info or verbose, you'll see messages prefixed with "FastUpToDate:" in the "Build" section of the "Output" pane in Visual Studio. These messages will explain why the project is considered up to date or not.

Uncheck the check box to disable the fast up to date check and always call MSBuild.