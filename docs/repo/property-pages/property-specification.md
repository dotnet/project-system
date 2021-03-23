# Property Specification

This document details how properties are specified, which controls their appearance and behavior in the Project Properties UI and how they are written to the project file.

## XAML Rule Files

The set of properties to display in the UI are outlined declaratively in XAML files that ship with the Project System. This means that most modifications to the Project Properties UI can be achieved by simply modifying an XML file.

Here we will walk through the structure of these files. Some familiarity with XAML rule files is assumed. We will not discuss data sources here.

Each XAML rule file describes a single page of properties. Multiple pages are displayed at once, so there are multiple rule files involved in the end-to-end experience. 

Here is a complex example of a string property that demonstrates the majority of features we will discuss below.

```xml
<StringProperty Name="MyProperty"
                DisplayName="My property"
                Description="This is my property."
                HelpUrl="https://example.com/all-about-my-property"
                Category="MyCategory">
  <StringProperty.Metadata>
    <NameValuePair Name="DependsOn" Value="OtherPage::OtherProperty;OtherPage::AnotherProperty" />
    <NameValuePair Name="VisibilityCondition">
      <NameValuePair.Value>(eq "SomeValue" (evaluated "OtherPage" "OtherProperty"))</NameValuePair.Value>
    </NameValuePair>
  </StringProperty.Metadata>
</StringProperty>
```

Breaking this down:

- The outer element specifies the _property type_ (see [Property Types](#Property-Types))
- The property defines some metadata values
  - `DependsOn` (optional) lists properties that may influence this property's values (see [Property Dependencies](#Property-Dependencies))
  - `VisibilityCondition` (optional) holds an expression that the UI will use to determine whether the property should be visible (see [Visibility Conditions](visibility-conditions.md))

## Property Types

Each property in the system has an associated type. For example, the _Assembly name_ property's value is a string, while the _Output type_ property's value is one of a set of enum values.

In the XAML rule files, properties are specified with an underlying type. These types are [defined by MSBuild](https://github.com/dotnet/msbuild/tree/master/src/Framework/XamlTypes) and are:

- `StringProperty`
- `StringListProperty`
- `BoolProperty`
- `EnumProperty`
- `DynamicEnumProperty`
- `IntProperty`

## Property Editors

The Project Properties UI maps each of the above property types to a default editor for the underlying type. This default can be overridden in the property's metadata.

Properties may specify additional metadata to modify and/or configure the editor used in the UI. See [property specification](property-specification.md) for further information.

The UI ships a default editor for each of the available property types. 

### Custom Editors

If a non-standard editor is required for a given property, one may be provided via MEF.

⚠ Editor extensibility is under development, tracked by https://github.com/dotnet/project-system/issues/6895. This section will be updated when the issue is resolved.

Using custom editors requires both specifying the editor to use in the XAML rule file (along with any metadata is consumes), as well as ensuring an editor with corresponding name is exported in the client.

To specify a custom editor, add to the property's `ValueEditors` collection in XAML:

```xml
<StringProperty Name="MyProperty">
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="MyEditor">
      <ValueEditor.Metadata>
        <NameValuePair Name="Key" Value="Value" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

If the `ValueEditors` collection contains multiple entries, the first one having a matching editor on the client is used. If no matching editors are found, the default property editor for the underlying property type (string, in this example) is used.

To expose an editor, export an instance of `IPropertyEditor`, setting the `Name` metadata on the export to match the property's `ValueEditor` `EditorType`.

Continuing the above example:

```c#
[Export(typeof(IPropertyEditor))]
[ExportMetadata("Name", "MyEditor")]
internal sealed class MyPropertyEditor : IPropertyEditor
{
    // ...
}
```

The `IPropertyEditor` is quite thoroughly documented. See that API documentation for guidance on implementing the interface correctly.

Note that the `IPropertyEditor` class is defined in CPS (Microsoft internal) however the documentation is available via IntelliSense.

## Property Dependencies

A property's metadata may declare zero or more other properties that it depends upon. This relationship is used solely for the purposes of making one property uneditable while an update to a property that it depends upon is in flight.

The problem this feature is trying to address is this: imagine property _B_ is computed from the value of property _A_. If the user updates _A_, then tabs to _B_ and makes a quick edit, there is a race condition between the user's edit of _B_ and the server's re-evaluation of _B_.

To address this issue, a property may declare that it depends upon the value of some other property. When that upstream dependency is being modified, any downstream dependencies are made non-editable in the UI, avoiding this race condition.

Most property updates are very fast, so this feature is only expected to help in cases where an update is unexpectedly slow, or the user is unexpectedly fast.

## Link Actions

It is useful to insert hyperlinks between properties that either link to URLs or perform arbitrary actions. This can be achieved via the `ActionLink` editor type. The underlying property type does not matter, as no value is presented.

Clicking the hyperlink can either open a URL or invoke a callback.

The property's `ValueEditor` has `EditorType="LinkAction"` which selects a UI that presents itself as a hyperlink, showing the `DisplayName` as the hyperlinked text. If a `Description` is also present, then the `DisplayName` is used as a heading and the description is the hyperlinked text.

### Open URL on Click

If a hyperlink should open a URL, the following template may be used.

The editor must specify two metadata values:

- `Action`, with value `URL`
- `URL`, with an HTTP or HTTPS URL value

```xml
<StringProperty Name="MyUrlProperty"
                DisplayName="Click me to open a URL">
  <StringProperty.DataSource>
    <DataSource PersistedName="MyUrlProperty"
                Persistence="ProjectFileWithInterception"
                HasConfigurationCondition="False" />
  </StringProperty.DataSource>
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="LinkAction">
      <ValueEditor.Metadata>
        <NameValuePair Name="Action" Value="URL" />
        <NameValuePair Name="URL" Value="https://some/website" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

This goes in concert with the export of the corresponding no-op interception code:

```c#
[ExportInterceptingPropertyValueProvider("MyUrlProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyUrlValueProvider : NoOpInterceptingPropertyValueProvider
{
}
```

### Callback on Click

If a hyperlink should call back into code, the following template may be used.

The editor must specify two metadata values:

- `Action`, with value `Command`
- `Command`, string for which an `ILinkActionHandler` is exported

```xml
<StringProperty Name="MyCommandProperty"
                DisplayName="Click me to do something">
  <StringProperty.DataSource>
    <DataSource PersistedName="MyCommandProperty"
                Persistence="ProjectFileWithInterception"
                HasConfigurationCondition="False" />
  </StringProperty.DataSource>
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="LinkAction">
      <ValueEditor.Metadata>
        <NameValuePair Name="Action" Value="Command" />
        <NameValuePair Name="Command" Value="MyCommandName" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

This goes in concert with the export of the corresponding no-op interception code:

```c#
[ExportInterceptingPropertyValueProvider("MyCommandProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyUrlValueProvider : NoOpInterceptingPropertyValueProvider
{
}
```

The click handler is exported via:

```c#
[Export(typeof(ILinkActionHandler))]
[ExportMetadata("CommandName", "MyCommandName")]
internal sealed class MyCommandActionHandler : ILinkActionHandler
{
    public void Handle(IReadOnlyDictionary<string, string> editorMetadata)
    {
        // Handle command invocation
    }
}
```

## File and Directory Properties

When a property's value represents a file or directory path, it should be modelled as a `StringProperty` with its `Subtype` attribute set to `file` or `directory` respectively.

```xml
<StringProperty Subtype="file"
                ...>
```

This will produce an editor that comprises a text box and _Browse_ button, which launches a file or directory picker dialog.

## Synthetic Properties

Sometimes it is useful to present the user with a property that does not directly map to any persisted property value. For example, there may be multiple modes that a feature works in, where each mode has a set of properties that apply only during that mode. From the user's perspective, it may make sense to present a synthetic property that selects the mode to use, which then causes only properties relevant to that mode to appear on screen. This synthetic property is not read or written to the project file.

It is possible to achieve this by authoring a property whose data source uses `Persistence="ProjectFileWithInterception"`, and exporting an appropriate `InterceptingPropertyValueProviderBase` subclass. See `PackageLicenseKindValueProvider` for an example of this pattern.

## Localization

XAML files in the dotnet/project-system repo are configured for automatic localization via XLF files, which are automatically generated and updated during build via [xliff-tasks](https://github.com/dotnet/xliff-tasks) MSBuild tasks/targets.

## Examples

- [Implement WarningsNotAsErrors in the new property pages](https://github.com/dotnet/project-system/pull/6971) - Demonstrates the addition of a new property, the use of `VisibilityCondition` and `DependsOn` metadata, and the implementation of an `IInterceptingPropertyValueProvider`. Includes an extensive explanation of the change in the commit message.
- [Add search term alias](https://github.com/dotnet/project-system/pull/7041) &mdash; shows how to add additional terms for the purposes of search. These terms will not appear in the UI, but will cause a search operation to match the property. Useful for synonyms and common misspellings.