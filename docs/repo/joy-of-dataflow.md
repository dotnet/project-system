# The Joy of Dataflow

Recipes for doing all sorts of things with System.Threading.Dataflow in the Project System.

## How to create a new data source

Use this recipe when you want to expose a fundamentally new _type_ of data that isn't available from some existing dataflow.

Do not use this recipe if the output data can be produced by a 1-to-1 transformation of an existing dataflow, or by subsetting or filtering an existing dataflow.

> TODO: Link to the appropriate recipes for transforming and filtering dataflows.

### Steps

1. Create a new class that derives from the `Microsoft.VisualStudio.ProjectSystem.ProjectValueDataSourceBase<T>` type in CPS, where `T` is the type of data you want to expose.
2. Override and implement the `DataSourceVersion` property. CPS dataflows typically pass around versioned snapshots of data (i.e. `IProjectVersionedValue<T>` where `T` is the actual data type); this ensures data consistency when multiple dataflows are combined into one. This property should return a `long` value that is incremented every time the data source's data changes.
3. Override and implement the `DataSourceKey` property. The key differentiates one data source version from another. It should be a unique value that is consistent across all versions of the data source. Typically this will get return a `NamedIdentity(nameof(_implementing type_))`.
4. Override and implement the `Initialize` method. This is where the dataflow block(s) will be created and where you will perform any setup required to start producing data.
    1. Call `base.Initialize()` to ensure the base class is initialized.
    2. Create your dataflow block using one of the `DataflowBlockSlim.Create*` methods. Store this block in a field (typically this is named `_broadcastBlock`).
    3. Creating the "public" view of the dataflow block using the `SafePublicize()` extension method on the block created in the previous step, and store this block in a field (typically the field is named `_publicBlock`). This is the block that will be exposed to consumers of the data source. The `SafePublicize()` method ensures that consumers cannot fault or complete the block; this would cause the data source to stop producing data, and typically we do not want other code to have control of that.
    4. Perform any other setup required to start producing data.
5. Override and implement the `SourceBlock` property. This should call `EnsureInitialized()` (to guarantee that `Initialize` method is called) and then return the `_publicBlock` field.
6. Implement your logic for pushing information to the `_broadcastBlock`. Typically this will include:
    1. Incrementing the version number.
    2. Creating an `ImmutableDictionary<NamedIdentity, IComparable>` from the `DataSourceKey` and the version number.
    3. Creating a `ProjectVersionedValue<T>` from the data and the version number.
    4. Calling `Post()` on the `_broadcastBlock` with the `ProjectVersionedValue<T>` created in the previous step.

### Examples

- [`LaunchSettingsProvider`](../../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/LaunchSettingsProvider.cs)
- [`DesignTimeInputsFileWatcher`](..\../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/TempPE/DesignTimeInputsFileWatcher.cs)

### Remarks

Note that both example not only produce data, they also consume data from other dataflows. However, their output data cannot be produced solely by transforming the inputs, so they are still considered "new" data sources.