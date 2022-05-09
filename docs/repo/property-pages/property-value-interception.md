# Property Value Interception

Sometimes it is necessary to intercept reads and writes of property values, and modify those values while they are in flight. Property value interception allows this.

Examples of why you may wish to do this:

- You wish to [provide a default value](#providing-default-values) for the property in the UI, when no value is provided in props/targets.
- The property is a [pseudo property](#pseudo-properties) and should not be written to the project. Its values may be ignored, or may actually be derived from and modify other properties instead.

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

## Intercepting multiple properties

A single interceptor class may be used to intercept multiple properties. To do that, pass an array containing multiple property names to the `ExportInterceptingPropertyValueProvider` attribute, such as:

```c#
[ExportInterceptingPropertyValueProvider(new[] { "Prop1", "Prop2" }, ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyPropertyValueProvider : InterceptingPropertyValueProviderBase
{
    // ...
}
```

## Base classes

While you can implement `IInterceptingPropertyValueProvider` directly, you may like to use one of these base types.

- `InterceptingPropertyValueProviderBase` has virtual methods you can overload, with base implementations that just forward values unchanged.
- `NoOpInterceptingPropertyValueProvider` fixes the value as null or the empty string. Useful when a property should never be written to the project file. See [Fixed content properties](#fixed-content-properties) for more.

## Providing default values

Consider a boolean (check box) property that should default to `true` when no value is specified. You can achieve this with something like the following:

```c#
[ExportInterceptingPropertyValueProvider("MyProperty", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class MyPropertyValueProvider : InterceptingPropertyValueProviderBase
{
    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        if (string.IsNullOrEmpty(unevaluatedPropertyValue))
        {
            // No value has been specified. Default to true.
            return Task.FromResult(bool.TrueString);
        }

        // Pass the original value, unmodified.
        return Task.FromResult(unevaluatedPropertyValue);
    }
}
```

## Pseudo-properties

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

### Mapping properties

Sometimes a feature requires more than one property to be set, but it would be clearer from the user's perspective to have a single property in the UI. We can use property value interception to achieve this.

Let's look at an example. The UI will display a checkbox for property `A`. Let's call this our logical property. Toggling that checkbox will cause either `B` or `C` to be present in the project. We'll call those our physical properties.

- When `A` is checked, `B` will be stored with some constant value, and `C` will be deleted.
- When `A` is unchecked, `C` will be stored with some constant value, and `B` will be deleted.

You can implement other policies, but this example shows the basic approach.

Our interceptor would be implemented as follows:

```c#
[ExportInterceptingPropertyValueProvider("A", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal sealed class AValueProvider : InterceptingPropertyValueProviderBase
{
    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        if (bool.TryParse(unevaluatedPropertyValue, out bool value))
        {
            if (value)
            {
                // A is checked. We set B and delete C.
                await defaultProperties.SetPropertyValueAsync("B", "some value");
                await defaultProperties.DeletePropertyAsync("C");
            }
            else
            {
                // A is unchecked. We set C and delete B.
                await defaultProperties.SetPropertyValueAsync("C", "some value");
                await defaultProperties.DeletePropertyAsync("B");
            }
        }

        // Return null here to prevent any value for A being written to the project file.
        return null;
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return ComputeValueAsync(defaultProperties.GetEvaluatedPropertyValueAsync!);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return ComputeValueAsync(defaultProperties.GetUnevaluatedPropertyValueAsync);
    }

    private static async Task<string> ComputeValueAsync(Func<string, Task<string?>> getValue)
    {
        string? b = await getValue("B");

        // If B has a value, consider A as checked (return true).
        // You may wish to check for a specific value, or explicitly handle the case where both B and C have values.
        return string.IsNullOrEmpty(b) ? bool.FalseString : bool.TrueString;
    }
}
```

Note that it's also possible for the `OnSetPropertyValueAsync` method to read the current state of other property values via `defaultProperties.GetUnevaluatedPropertyValueAsync`.
