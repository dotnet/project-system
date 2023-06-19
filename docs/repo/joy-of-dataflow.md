# The Joy of Dataflow

Recipes for doing all sorts of things with System.Threading.Dataflow in the Project System.

## Table of Contents

- [How to create an `IProjectValueDataSource` that produces data derived from existing sources](#how-to-create-an-iprojectvaluedatasource-that-produces-data-derived-from-existing-sources)
- [How to create an `IProjectValueDataSource` that produces original data](#how-to-create-an-iprojectvaluedatasource-that-produces-original-data)

## How to create an `IProjectValueDataSource` that produces data derived from existing sources

Use this recipe when you want to expose a subset of the data from an existing dataflow, or when you want to transform the data from an existing dataflow into a different shape.

### Steps

1. Create a new class that derives from the `Microsoft.VisualStudio.ProjectSystem.ChainedProjectValueDataSourceBase<T>` in CPS, where `T` is the type of data you want to expose.
2. In your `[ImportingConstructor]` import the dataflow you want to transform/filter.
3. Override and implement the `LinkExternalInput` method. This is where the dataflow block(s) will be created and where you will perform any setup required to start producing data.
    1. Use `DataflowBlockSlim.CreateTransformBlock` to create a transform block that takes in `IProjectVersionedValue<U>` (where `U` is the input data) and produces `IProjectVersionedValue<T>` (again, `T` is the type of data you want to expose).
    2. Since the intent here is to transfrom data from existing sources the version keys and values for your output data will generally be the same as the input. To avoid boilerplate code around `IProjectVersionedValue<>` handling, pass in the lambda `update => update.Derive(Transform)` for the `transformFunction` parameter of `CreateTransformBlock`. The `Derive` extension method handles calling the `Transform` method (to be defined in a later step) to convert `U`s to `T`s and producing the final `IProjectVersionedValue<>` with the version keys and values copied over.
    3. Using the `LinkTo` extension method, link your input dataflow to the transform block and the transform block to the `targetBlock` passed in to `LinkExternalInput`. Store the resulting link objects in a `DisposableBag`.
    4. Call `JoinUpstreamDataSources` and pass in your input data sources. This links the scheduling of background work across the data flows and helps prevent deadlocks. Storing the resulting object in a `DisposableBag`.
    5. Finally, return the `DisposableBag`. When your dataflow is no longer needed the bag, and all its contents, will be disposed. This will break the links between the blocks as well as the scheduling links to the input data flows.
4. Finally, define your `Transform` function. This will take in an instance of type `U` and produce exactly one instance of the output type `T`.

### Examples

- [`DebugProfileDebugTargetGenerator`](../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/DebugProfileDebugTargetGenerator.cs): A very simple example that converts a dataflow of launch settings into the list of launch profiles for display to the user.

### Remarks

Sometimes you may want to produce multiple output items for a single input, or zero items. For example, a dataflow that may need to hold production of items until some condition is met. In this case you can use `DataflowBlockSlim.CreateTransformManyBlock` instead of `CreateTransformBlock`, and your `Transform` function will return `IEnumerable<T>` instead of `T`.

## How to create an `IProjectValueDataSource` that produces original data

Use this recipe when you want to expose a fundamentally new _type_ of data that isn't available from some existing dataflow. Note this is very rare and unlikely to be what you want or need; most of the time you'll want to derive from the output of existing dataflow blocks. See [How to create an `IProjectValueDataSource` that produces data derived from existing sources](#how-to-create-an-iprojectvaluedatasource-that-produces-data-derived-from-existing-sources) for how to do that.

In practice this approach is used when you want a dataflow exposing the _contents_ of a file that may change over time.

### Steps

1. Create a new class that derives from the `Microsoft.VisualStudio.ProjectSystem.ProjectValueDataSourceBase<T>` type in CPS, where `T` is the type of data you want to expose.
2. Override and implement the `DataSourceVersion` property. CPS dataflows typically pass around versioned snapshots of data (i.e. `IProjectVersionedValue<T>` where `T` is the actual data type); this ensures data consistency when multiple dataflows are combined into one. This property should return a `long` value that is incremented every time the data source's data changes.
3. Override and implement the `DataSourceKey` property. The key differentiates one data source version from another. It should be a unique value that is consistent across all versions of the data source. Typically this will return a `NamedIdentity(nameof(_implementing type_))`.
4. Override and implement the `Initialize` method. This is where the dataflow block(s) will be created and where you will perform any setup required to start producing data.
    1. Call `base.Initialize()` to ensure the base class is initialized.
    2. Create your dataflow block using one of the `DataflowBlockSlim.Create*` methods. Store this block in a field (typically this is named `_broadcastBlock`).
    3. Create the "public" view of the dataflow block using the `SafePublicize()` extension method on the block created in the previous step, and store this block in a field (typically the field is named `_publicBlock`). This is the block that will be exposed to consumers of the data source. The `SafePublicize()` method creates a "wrapper" around the block that turns calls to `Complete()` and `Fault(Exception)` into no-ops. Completing or faulting a block prevents it from accepting or producing data, and within the project system only the type responsible for creating the block should have that level of control.
    4. Perform any other setup required to start producing data.
5. Override and implement the `SourceBlock` property. This should call `EnsureInitialized()` (to guarantee that `Initialize` method is called) and then return the `_publicBlock` field.
6. Implement your logic for pushing information to the `_broadcastBlock`. Typically this will include:
    1. Incrementing the version number.
    2. Creating an `ImmutableDictionary<NamedIdentity, IComparable>` from the `DataSourceKey` and the version number.
    3. Creating a `ProjectVersionedValue<T>` from the data and the version number.
    4. Calling `Post()` on the `_broadcastBlock` with the `ProjectVersionedValue<T>` created in the previous step.

### Examples

- [`LaunchSettingsProvider`](../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/LaunchSettingsProvider.cs)
- [`DesignTimeInputsFileWatcher`](../../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/TempPE/DesignTimeInputsFileWatcher.cs)

### Remarks

Note that both examples not only produce data, they also consume data from other dataflows. However, their output data cannot be produced solely by transforming the inputs, so they are still considered "new" data sources.

