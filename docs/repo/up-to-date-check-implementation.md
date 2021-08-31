# Up-to-date Check Implementation

See [this documenent](../up-to-date-check.md) for more general information about the up-to-date check.

The `IBuildUpToDateCheckProvider` interface (from CPS) has two members:

- `IsUpToDateCheckEnabledAsync`, which is serviced by `IProjectSystemOptions.GetFastUpToDateLoggingLevelAsync`
- `IsUpToDateAsync` which performs the checks described below

We implement the derived `IBuildUpToDateCheckProvider2` version of the interface, which gives us access to the set of global properties used in the build. A build may use global properties to exlude certain "kinds" of inputs and outputs if necessary.

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

## Persistence

Our up-to-date check persists data to the `.vs` folder, in a binary file such as `.vs/MySolution/v17/.futdcache.v1`.

This file contains, per project:

1. A hash of the items in the project, so that we can tell whether the items have changed since the last time we loaded the project. If not, we can consider the project up-to-date on solution open, which can save a lot of time for the user's first build/test/debug operations.
2. The time at which the last set of items was observed to have changed. This is also important on solution open, to correctly handle the first up-to-date check of a project.

## Implementation

- [`BuildUpToDateCheck.cs`](/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/UpToDate/BuildUpToDateCheck.cs)
- [`BuildUpToDateCheckTests.cs`](/tests/Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests/ProjectSystem/UpToDate/BuildUpToDateCheckTests.cs)
