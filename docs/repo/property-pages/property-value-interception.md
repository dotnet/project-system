# Property Value Interception

Sometimes it is necessary to intercept reads and writes of property values, and modify those values while they are in flight. Property value interception allows this.

## Mechanism

The property must opt in to interception. Its `DataSource` must specify the `Persistence` property as one of:

- `ProjectFileWithInterception` writes values to the project file.
- `UserFileWithInterception` writes values to the project's `.user` file.

For example:

```xml
<StringProperty Name="MyProperty"
                DisplayName="My property"
                ...>
  <StringProperty.DataSource>
    <DataSource PersistedName="MyProperty"
                Persistence="ProjectFileWithInterception"
                HasConfigurationCondition="False"
                ... />
  </StringProperty.DataSource>
  ...
</StringProperty>
```

> See [Property Specification](property-specification.md) for more information on specifying properties.

There must be a corresponding export of `IInterceptingPropertyValueProvider` for the property. For example:

```c#
[ExportInterceptingPropertyValueProvider("MyProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyPropertyValueProvider : InterceptingPropertyValueProviderBase
{
    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return Task.FromResult("❤️"); // This property's value is always ❤️
    }
}
```

⚠️ The property name passed in the `ExportInterceptingPropertyValueProvider` attribute **must** match the property being intercepted.

The name of the class is not important, but you might like to follow the convention offered here.

## Base classes

While you can implement `IInterceptingPropertyValueProvider` directly, you may like to use one of these base types.

- `InterceptingPropertyValueProviderBase` has virtual methods you can overload, with base implementations that just forward values unchanged.
- `NoOpInterceptingPropertyValueProvider` fixes the value as null or the empty string. Useful when a property should never be written to the project file. See [Fixed content properties](#fixed-content-properties) for more.

## Psuedo-properties

### Mapping properties

**TODO** explain how one psuedo-property can be mapped to multiple real properties

### Fixed content properties

It is convenient to add certain kinds of content to a page of properties, where that content is not actually a property. For example, a paragraph of text, or a hyperlink.

Such properties should never be read from or written to the project file. We can ensure this by intercepting the property with a no-op interceptor:

```c#
[ExportInterceptingPropertyValueProvider("MyProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyCommandPropertyValueProvider : NoOpInterceptingPropertyValueProvider
{
}
```

The base class `NoOpInterceptingPropertyValueProvider` blocks all writes and returns empty strings for all reads.
