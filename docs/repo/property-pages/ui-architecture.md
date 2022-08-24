# UI Architecture

Several components and concepts come together to produce the UI for the new Project Property UI. This document provides an outline of these elements and shows how they relate to one another. Where necessary, links to further information are provided. The source code for the UI is Microsoft internal, so if there are details you believe to be useful that are omitted here, please file an issue in this repo so that clarifications can be made.

## Editor Factory

The UI is exposed in Visual Studio as an editor via `ProjectPropertiesEditorFactory`. Editor factories are identified via their GUID, and this one uses `990036eb-f67a-4b8a-93d4-4663db2a1033`. It uses only a default logical and physical view.

When invoked, the editor produces an instance of `ProjectPropertiesEditor` which contains the UI to display.

## Editor

The `ProjectPropertiesEditor` class is a WPF `UserControl` that includes:

1. The list of properties (`PropertyList`).
2. A table of contents to allow quick navigation within properties (`NavigationTree`).
3. A search box to allow quick discovery of properties (`SearchBox`).

Its only logic is to call the back-end for property data when it is first constructed.

## Data Access

When first constructed, the editor uses an instance of `IPropertyDataAccess` to communicate with the back-end and ultimately produce a [`PropertyContext`](#property-context) object. This context contains the set of properties to display. This occurs via CPS's _Project Query API_.

## Property Context

The set of properties displayed in an editor are bundled together into a `PropertyContext` instance, which contains:

1. The set of properties, their values and metadata.
1. The matrix of configurations the project contains.
1. The ability to update the evaluated and unevaluated value of properties.
1. Logic to update the visibility of properties over time as required.
1. Logic to temporarily make related properties read-only while updates are in flight.
1. UI commands for modifying the property configurations.
1. A registry of callbacks for `LinkAction` editors.

## Properties

Each property in the UI is represented by an instance of the `Property` class, whose data breaks down into _metadata_ and _state_.

Properties are identified by the combination of their name, and the name of the page they are found on. Therefore each property must have a unique name within its page.

### Metadata

The metadata of a property is immutable and will not change over time. It is modelled by the `PropertyMetadata` class and contains:

1. `Name` the name of the property, as defined in the rule file. Not user visible.
1. `DisplayName` the name of the property to display to the user. Localized.
1. `Page` the page on which the property resides.
1. `Category` the category within the page on which the property resides.
1. `DependsOn` an optional list of property identifiers upon which this property is expected to depend. See [property dependencies](property-specification.md#property-dependencies).
1. `Description` an optional description to display to the user. Localized.
1. `Priority` an integer that controls the order of the property within the UI.
1. `Editor` the property editor to use for the property. See [property editors](property-specification.md#property-editors).
1. `EditorMetadata` a map of key/value pairs that is passed to the editor and which may modify the editor's behavior.
1. `SupportsPerConfigurationValues` whether the property may be configured or not. See [property configurations](#property-configurations).
1. `SearchTerms` an optional list of additional search terms for which the property should be displayed. Useful for synonyms, common misspellings, etc.
1. `HelpUrl` an optional URL for documentation about the property.
1. `VisibilityCondition` an optional expression that controls when the property is visible or hidden from view. See [visibility conditions](property-conditions.md).

This metadata comes from XAML rule files deployed with the project system. For information on authoring such metadata, see [Property Specification](property-specification.md).

### State

Each `Property` instance holds, in addition to a reference to its `PropertyMetadata`, several other pieces of state. Unlike that metadata, these state values may change over time in response to the user's actions:

1. The set of _dimensions_ the property varies by.
1. The single _value_, or multiple _values_ if the property varies by configuration.
1. The _visibility_ of the property, influenced by both [visibility conditions](property-conditions.md) and property search.

## Unevaluated and Evaluated Values

Most Visual Studio projects are specified as using MSBuild. The MSBuild language allows for its property values to be specified using an expression language, where that expression may include MSBuild syntax for property substitution and intrinsic function calls.

In the following example, the `$(...)` syntax causes the value of `SomeOtherProperty` to be inserted inline:

```xml
<PropertyGroup>
  <MyProperty>$(SomeOtherProperty)_WithSuffix</MyProperty>
</PropertyGroup>
```

During a build, this property is _evaluated_ to produce the final value. In this way we distinguish between a property's _unevaluated_ value and its _evaluated_ value.

For simple expressions, the unevaluated and evaluated value will be the same. However if the property is defined using MSBuild syntax, then there will be a difference. When such a difference exists for a string property, the new Project Properties UI will display both the unevaluated and evaluated value on screen. Only the unevaluated value may be edited, while the evaluated value helps the user to see the computation of their expression to its ultimate value. 

Furthermore, the evaluation of a property occurs _per configuration_, so there may be more than one evaluated value for a given unevaluated value expression. The UI will show each of these, including details of the configuration that produced each value.

The disploay of unevaluated and evaluated values is currently limited to string properties. For example, if a `BoolProperty` is defined using the MSBuild intrinsic function `$(MSBuild::IsOsUnixLike())`, the UI will show a check box with no indication of this expression. Toggling the check state of that property will replace or override the property with a literal boolean value.

## Property Configurations

Projects generally have multiple configurations. In fact, the project has a configuration matrix. This matrix has multiple dimensions (e.g. configuration, platform, target framework). Each dimension has one or more values (e.g. configuration common supports both _Release_ and _Build_). If the project is multi-targeting, then the target framework dimension will be populated with the project's target frameworks.

Some properties must necessarily be the same across all configurations. For example, the default namespace or assembly name do not vary by configuration.

For other properties, it may make sense to vary the property's value by one or more configuration dimension. For example, the output should be optimized in release builds and not in debug builds.

The Project Properties UI allows the user to control how values vary across dimensions.

## Visibility and Property Conditions

See [Visibility and Property Conditions](property-conditions.md).

## Property Simplification

Recall that a project has a _configuration matrix_ and that a property's value may vary by project configuration. When the UI queries the server for a property's values, a value per configuration is returned. Showing all values to the user would be overwhelming, adding a lot of visual noise to the UI and making it difficult to apply a single value across all configurations.

The Project Properties UI has a process for "simplifying" property values. This process is largely invisible to the user, as it produces a result that matches the user's expectation. The implementation of this simplification in `PropertyValue.Create` is surprisingly complex, and is therefore thoroughly unit tested.

At a high level, the simplification is a pure function that takes as inputs:

1. A property value per possible project configuration.
1. The set of configuration dimensions the user has manually selected to vary the property's value by.
1. The project's configuration matrix.
1. Whether the property shows unevaluated and evaluated values, or only evaluated values.
1. A callback function that indicates whether the value was changed by evaluation (as this is dependent upon the property's type).

This function returns a tuple of:

1. The property values to display in the UI, with aggregation over evaluated values in the case that a single unevaluated value produces multiple evaluated values across configurations.
1. The updated set of "vary by" dimensions, in case the user's manually selected set of dimensions is inadequate to capture the variation inherent in the current property values.
