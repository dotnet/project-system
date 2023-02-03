# Property Specification

This document details how properties are specified, which controls their appearance and behavior in the Project Properties UI and how they are written to the project file.

## Examples

Firstly, here are some commits and PRs that provide good examples of common changes you might make to a set of properties:

- [Add `WarningsNotAsErrors` property](https://github.com/dotnet/project-system/pull/6971) &mdash; Demonstrates the addition of a new property, the use of `VisibilityCondition` and `DependsOn` metadata, and the implementation of an `IInterceptingPropertyValueProvider`. Includes an extensive explanation of the change in the commit message.
- [Reorder properties within a page](https://github.com/dotnet/project-system/pull/7038) &mdash; Demonstrates reordering properties within a single category on a single page. This simple change is made entirely within a XAML rule file.
- [Add a new page of properties](https://github.com/dotnet/project-system/commit/a442d8e91fec98cb493d924f0903308efe188344) &mdash; Adds a new, empty, page that will appear as a top-level navigation item in the Project Properties UI.
- [Add a paragraph between properties](https://github.com/dotnet/project-system/commit/64b7693e104a725fc0ac9d2bbda76909d9a7b9d1) &mdash; Adds a single synthetic property which appears in the UI as a fixed (localized) block of text.
- [Add search term alias](https://github.com/dotnet/project-system/pull/7041) &mdash; shows how to add additional terms for the purposes of search. These terms will not appear in the UI, but will cause a search operation to match the property. Useful for synonyms and common misspellings.
- [Add an editor type](https://devdiv.visualstudio.com/DevDiv/_git/CPS/pullrequest/312423) (MS internal) &mdash; Adds a new editor type, which a property may elect to display itself in the UI.
- [Change a string property to allow multiple lines of text](https://github.com/dotnet/project-system/commit/5a37eb52aeb93ae5f8a13c2cccfde79ae371a9ac) &mdash; Shows using the `MultiLineString` editor so that a property may have more than one line of text entered.
- [Use editability (IsReadOnlyCondition) conditions in build property pages](https://github.com/dotnet/project-system/pull/8291) &mdash; Shows using the `IsReadOnlyCondition` property condition.

## XAML Rule Files

The set of properties to display in the UI are outlined declaratively in XAML files. This means that most modifications to the Project Properties UI can be achieved by simply modifying an XML file.

### Adding rules as MSBuild items

Any rule file to be included in the UI must be added to the project's evaluation. Each file must be added with an item of type `PropertyPageSchema`. For example in a `.props` or `.targets` file:

```xml
<ItemGroup>
  <PropertyPageSchema Include="$(MSBuildThisFileDirectory)\$(LocaleFolder)MyProjectPropertiesPage.xaml">
    <Context>Project</Context>
  </PropertyPageSchema>
</ItemGroup>
```

A couple of things to note:

- Rule files contain display strings which must be localised. Depending upon how you produce your package, you will want to make sure that the localised file is included.
- The "Context" metadata must be set to "Project". Rule files have other uses (unrelated to the property page UI) in other parts of the project system; correct "Context" metadata is important to ensure they are only used as intended.

### Structure of a XAML rule file

Here we will walk through the structure of these files. Some familiarity with XAML rule files is assumed. We will not discuss data sources here.

Each XAML rule file describes a single page of properties. Multiple pages are displayed at once, so there are multiple rule files involved in the end-to-end experience. 

```xml
<Rule Name="MyProjectPropertiesPage"
      Description="A description of my project properties page."
      DisplayName="My Properties"
      PageTemplate="generic"
      Order="500"
      xmlns="http://schemas.microsoft.com/build/2009/properties">

  <Rule.DataSource>
    <DataSource Persistence="ProjectFile"
                SourceOfDefaultValue="AfterContext"
                HasConfigurationCondition="False" />
  </Rule.DataSource>

  <!-- TODO add properties here -->

</Rule>
```

- `PageTemplate` must be `generic` for project properties, or `commandNameBasedDebugger` for launch profiles.
- `Order` controls ordering of pages in the UI. Lower numbers appear towards the top, with the one exception of pages having zero order appearing at the end (to prevent pages accidentally appearing in prime position).
- `DisplayName` value will appear in group headings and the navigation tree.
- `Description` is currently unused.

The `DataSource` specified here will be applied to all properties, however properties may override data source properties as needed.

- `Persistence` may have several values:
  - `ProjectFile` means that the value will be read and written from the project file directly.
  - `ProjectFileWithInterception` means that a MEF part exists that will handle read/write operations for the property (see below).
  - `UserFile` means that the value will be read and written from the `.user` file directly.
  - `UserFileWithInterception` is the same as `ProjectFileWithInterception` except we write changes to the project's `.user` file.
  - `LaunchProfile` means that the value will be read and written from the `launchSettings.json` file directly.
- `HasConfigurationCondition` controls whether the property is intended to be varied by project configuration (e.g. Debug/Release, platform, target framework...). Setting this to true allows varying property values by configuration dimensions.

### Categories

Unless otherwise specified, each property will be placed into the `General` category.

If you wish to assign properties to specific categories, you must declare them up-front as follows:

```xml
  <Rule.Categories>
    <Category Name="General"
              DisplayName="General"
              Description="General settings for the application." />
    <Category Name="Resources"
              DisplayName="Resources"
              Description="Resource settings for the application." />
  </Rule.Categories>
```

- `Name` is a non-visible identifier, used in properties' `Category` attributes.
- `DisplayName` value will appear in group headings and the navigation tree.
- `Description` is currently unused.

### An example property

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
      <NameValuePair.Value>(has-evaluated-value "OtherPage" "OtherProperty" "SomeValue")</NameValuePair.Value>
    </NameValuePair>
    <NameValuePair Name="IsReadOnlyCondition">
      <NameValuePair.Value>
          (not
            (or
              (has-evaluated-value "Build" "OptionStrict" "On")
              (has-evaluated-value "Build" "WarningSeverity" "DisableAll")
            )
          )
      </NameValuePair.Value>
    </NameValuePair>  
  </StringProperty.Metadata>
</StringProperty>
```

Breaking this down:

- The outer element specifies the [property type](#property-types) (`StringProperty` in this example).
- `DisplayName` and `Description` are localised values that will appear in the UI.
- `HelpUrl` is an optional URL that causes a help icon to appear next to the property's name. For Microsoft components, this should be a fwlink, to allow fixing dead links in future.
- `Category` is an optional string that must match the `Name` of a declared category (see above). If omitted, the property is assigned category `General`.
- This property defines some optional metadata values:
  - `DependsOn` (optional) lists properties that may influence this property's values (see [Property Dependencies](#property-dependencies))
  - `VisibilityCondition` (optional) holds an expression that the UI will use to determine whether the property should be visible (see [Visibility and Property Conditions](property-conditions.md))
  - `IsReadOnlyCondition` (optional) holds an expression that the UI will use to determine whether the property should be read-only (see [Visibility and Property Conditions](property-conditions.md))

## Property Types

Each property in the system has an associated type. For example, the _Assembly name_ property's value is a string, while the _Output type_ property's value is one of a set of enum values.

In the XAML rule files, properties are specified with an underlying type. These types are [defined by MSBuild](https://github.com/dotnet/msbuild/tree/master/src/Framework/XamlTypes) and are:

- `StringProperty` (these can be [validated with the use of a regular expression](string-property-validation.md))
- `StringListProperty`
- `BoolProperty`
- `EnumProperty`
- `DynamicEnumProperty`
- `IntProperty`

## Property Editors

The Project Properties UI maps each of the above property types to a default editor for the underlying type. This default can be overridden in the property's metadata.

Properties may specify additional metadata to modify and/or configure the editor used in the UI. See [property specification](property-specification.md) for further information.

The UI ships a default editor for each of the available property types.

### Multi-line Strings

Most `StringProperty` properties are expected to have short values that fit on one line.

If a property is expected to have multiple lines of text, the editor should be changed to `MultiLineString`. For example:

```xml
<StringProperty Name="MyMultiLineProperty"
                DisplayName="A Multi-line Property"
                Description="A property that may contain multiple lines of text.">
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="MultiLineString" />
  </StringProperty.ValueEditors>
</StringProperty>
```

If the text within this editor should be displayed with a monospace (fixed width) font, as would be common for code or other text which might be expected to align by column, add `UseMonospaceFont` following metadata with a value of `True`:

```xml
<StringProperty Name="MyMultiLineProperty"
                DisplayName="A Multi-line Property"
                Description="A property that may contain multiple lines of text.">
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="MultiLineString">
      <ValueEditor.Metadata>
        <NameValuePair Name="UseMonospaceFont" Value="True" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

### Password Strings

A `PasswordBox` control can be used for string properties by specifying `EditorType="PasswordString"`.

### Evaluated-preview-only Strings

If you wish for a property to only display a non-editable preview of its evaluated values, you can use the `ShowEvaluatedPreviewOnly` editor metadata.

We use this on the `LangVersion` property, for example, as this value is intentionally non-editable, yet we want to allow the user to see the evaluated values. This value is specified by SDK targets, and so there is no useful unevaluated value to display for this property.

```xml
<StringProperty Name="LangVersion"
                DisplayName="Language version"
                Description="The version of the language available to code in this project."
                ReadOnly="true">
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="String">
      <ValueEditor.Metadata>
        <NameValuePair Name="ShowEvaluatedPreviewOnly" Value="True" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

### Name/Value List

When a property contains a variable number of name/value pairs, you can use the `NameValueList` editor on `StringProperty` to display a two-column grid in the UI that allows users to edit values and add/remove rows.

For example:

```xml
<StringProperty Name="EnvironmentVariables"
                DisplayName="Environment variables"
                Description="The environment variables to set prior to running the process.">
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="NameValueList" />
  </StringProperty.ValueEditors>
</StringProperty>
```

#### Encoding

By default, the property's string value will be encoded with format resembling `A=1,B=2`, using `/` as an escape character if needed. See `KeyValuePairListEncoding` in this repo for further details.

A custom encoding can be specified. It must be exported as an instance of `Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer.INameValuePairListEncoding` with the appropriate MEF metadata:

```xml
<StringProperty Name="MyProperty">
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="NameValueList">
      <ValueEditor.Metadata>
        <NameValuePair Name="Encoding" Value="MyEncodingName" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

For this to work, there would need to be an equivalent export of the `MyEncodingName` encoding:

```c#
[Export(typeof(INameValuePairListEncoding))]
[ExportMetadata("Encoding", "MyEncodingName")]
internal sealed class MyEncoding : INameValuePairListEncoding
{
    public IEnumerable<(string Name, string Value)> Parse(string value)
    {
        // TODO
    }

    public string Format(IEnumerable<(string Name, string Value)> pairs)
    {
        // TODO
    }
}
```

### Multi-String Selector Editor

When a property contains a variable number of strings, you can use the `MultiStringSelector` editor on a StringProperty or DynamicEnumProperty to display a list of checkable/uncheckable strings.

The `TypeDescriptorText` metadata must be included in order to describe what is actually being selected.

By default, users will not be able to add their own custom strings to the list. To allow custom strings, set the `AllowsCustomStrings` metadata value to True.

To show the evaluated value of the property below the multi-string selector, set the `ShouldDisplayEvaluatedPreview` metadata value to True.

#### StringProperty vs DynamicEnumProperty

If your multi-string selector control needs to include _non-selected_ items as part of the options, you should use 
a DynamicEnumProperty instead of a StringProperty. All `SupportedValues` that are not part of the unevaluated value 
will be added as unchecked list items.

#### Example

```xml
<StringProperty Name="ImportedNamespaces"
                DisplayName="Import Namespaces"
                Description="Manage which namespaces to import in your application."
                Category="General">
    <StringProperty.DataSource>
      <DataSource PersistedName="ImportedNamespaces"
                  Persistence="ProjectFileWithInterception"
                  HasConfigurationCondition="False" />
    </StringProperty.DataSource>
    <StringProperty.ValueEditors>
      <ValueEditor EditorType="MultiStringSelector">
        <ValueEditor.Metadata>
          <NameValuePair Name="TypeDescriptorText" Value="Imported Namespaces" xliff:LocalizableProperties="Value" />
          <NameValuePair Name="AllowsCustomStrings" Value="True" />
        </ValueEditor.Metadata>
      </ValueEditor>
    </StringProperty.ValueEditors>
</StringProperty> 
```

This example is taken from the [_Imported Namespaces_](https://github.com/dotnet/project-system/blob/1f860b8c3616a6be551f7a3f90eb54be3b249afd/src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Rules/PropertyPages/ReferencesPage.VisualBasic.xaml#L22) property on the Visual Basic project properties references page.

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

## Description Text

It can sometimes be useful to insert free-standing text within a series of properties. This can be achieved via the `Description` editor type. The underlying property type does not matter, as no value is presented.

The property's `ValueEditor` has `EditorType="Description"` which selects a UI that presents the property's `Description` value as a text block. The `DisplayName` is ignored. The user cannot interact with this text. It does participate in search.


```xml
<StringProperty Name="MyDescriptionProperty"
                DisplayName="Ignored"
                Description="This is the text that will appear in the UI.">
  <StringProperty.DataSource>
    <DataSource PersistedName="MyDescriptionProperty"
                Persistence="ProjectFileWithInterception"
                HasConfigurationCondition="False" />
  </StringProperty.DataSource>
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="Description" />
  </StringProperty.ValueEditors>
</StringProperty>
```

We don't want this property to ever be read from or written to the project file. We intercept these reads and writes by specifying `Persistence="ProjectFileWithInterception"`, and providing the following no-op interceptor. See [Property Value Interception](property-value-interception.md#pseudo-properties) for more on how and why this works.

```c#
[ExportInterceptingPropertyValueProvider("MyDescriptionProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyDescriptionPropertyValueProvider : NoOpInterceptingPropertyValueProvider
{
}
```

## Link Actions

It is useful to insert hyperlinks between properties that either: **link to URLs**, **perform arbitrary actions**, or **focus a property or property page/category**. This can be achieved via the `LinkAction` editor type. The underlying property type does not matter, as no value is presented.

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

We don't want this property to ever be read from or written to the project file. We intercept these reads and writes by specifying `Persistence="ProjectFileWithInterception"`, and providing the following no-op interceptor. See [Property Value Interception](property-value-interception.md#pseudo-properties) for more on how and why this works.

```c#
[ExportInterceptingPropertyValueProvider("MyUrlProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyUrlPropertyValueProvider : NoOpInterceptingPropertyValueProvider
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

We don't want this property to ever be read from or written to the project file. We intercept these reads and writes by specifying `Persistence="ProjectFileWithInterception"`, and providing the following no-op interceptor. See [Property Value Interception](property-value-interception.md#pseudo-properties) for more on how and why this works.

```c#
[ExportInterceptingPropertyValueProvider("MyCommandProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyCommandPropertyValueProvider : NoOpInterceptingPropertyValueProvider
{
}
```

Because we specified `Action` as `Command`, we must export a matching instance of `ILinkActionHandler` as follows:

```c#
[Export(typeof(ILinkActionHandler))]
[ExportMetadata("CommandName", "MyCommandName")]
internal sealed class MyCommandActionHandler : ILinkActionHandler
{
    public void Handle(UnconfiguredProject project, IReadOnlyDictionary<string, string> editorMetadata)
    {
        // Handle command invocation
    }
}
```

### Focus a property, property page, or property page category

If a hyperlink should, when clicked, set focus to a property page, property page category, or property, the following template may be used.

The editor must specify at least two metadata values:

- `Action`, with value `Focus`
- `PropertyPage`, with value being the `Name` attribute of the property page to focus.

If you wish to focus a property page category instead of a property page, you must also specify the `PropertyPageCategory` metadata value.

If you wish instead to focus a specific property, you must also specify the `Property` metadata value (**do not** specify `PropertyPageCategory` in this case).

```xml
<StringProperty Name="MyCommandProperty"
                DisplayName="Click me to focus a property page category">
  <StringProperty.DataSource>
    <DataSource PersistedName="MyCommandProperty"
                Persistence="ProjectFileWithInterception"
                HasConfigurationCondition="False" />
  </StringProperty.DataSource>
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="LinkAction">
      <ValueEditor.Metadata>
        <NameValuePair Name="Action" Value="Focus" />
        <NameValuePair Name="PropertyPage" Value="Build" />
        <NameValuePair Name="PropertyPageCategory" Value="Output" /> <!-- change metadata to Property to focus property, or remove to focus the Build page -->
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
</StringProperty>
```

We don't want this property to ever be read from or written to the project file. We intercept these reads and writes by specifying `Persistence="ProjectFileWithInterception"`, and providing the following no-op interceptor. See [Property Value Interception](property-value-interception.md#pseudo-properties) for more on how and why this works.

```c#
[ExportInterceptingPropertyValueProvider("MyCommandProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyCommandPropertyValueProvider : NoOpInterceptingPropertyValueProvider
{
}
```


## File Properties

When a property's value represents a file path, it should be modelled as a `StringProperty` with its `Subtype` attribute set to `file`.

```xml
<StringProperty Subtype="file"
                ...>
```

This will produce an editor that comprises a text box and _Browse_ button, which launches a file picker dialog.

To control the set of file extensions the user is allowed to select, add metadata resembling the following:

```xml
  <StringProperty.ValueEditors>
    <ValueEditor EditorType="FilePath">
      <ValueEditor.Metadata>
        <NameValuePair Name="FileTypeFilter" Value="Image files (*.png,*.jpg,*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*" />
      </ValueEditor.Metadata>
    </ValueEditor>
  </StringProperty.ValueEditors>
```

The format of the `FileTypeFilter` property is important, and invalid values will cause an exception when _Browse_ is clicked. Be sure to test your values. For information on this format, read [this documentation](https://docs.microsoft.com/dotnet/api/microsoft.win32.filedialog.filter?view=net-5.0).

## Directory Properties

When a property's value represents a directory path, it should be modelled as a `StringProperty` with its `Subtype` attribute set to `directory` (`folder` is also accepted, and is equivalent to `directory`).

```xml
<StringProperty Subtype="folder"
                ...>
```

This will produce an editor that comprises a text box and _Browse_ button, which launches a directory picker dialog.

## Synthetic Properties

Sometimes it is useful to present the user with a property that does not directly map to any persisted property value. For example, there may be multiple modes that a feature works in, where each mode has a set of properties that apply only during that mode. From the user's perspective, it may make sense to present a synthetic property that selects the mode to use, which then causes only properties relevant to that mode to appear on screen. This synthetic property is not read or written to the project file.

It is possible to achieve this by authoring a property whose data source uses `Persistence="ProjectFileWithInterception"`, and exporting an appropriate `InterceptingPropertyValueProviderBase` subclass. See `PackageLicenseKindValueProvider` for an example of this pattern.

## Localization

XAML files in the dotnet/project-system repo are configured for automatic localization via XLF files, which are automatically generated and updated during build via [xliff-tasks](https://github.com/dotnet/xliff-tasks) MSBuild tasks/targets.
