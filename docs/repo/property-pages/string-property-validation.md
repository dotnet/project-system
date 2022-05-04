# String property validation

It may be useful for the Project System, or producers of property pages, to add regular expression-based client-side validation to string properties.

For example, for a property `ApplicationDisplayVersion` that is at most a three-part version number, users would benefit from immediate feedback on whether their property value is valid.

## Specification

You must create a custom ValueEditor to opt into value validation. 

There is one required ValueEditor metadata property: `EvaluatedValueValidationRegex`, which is compared against the property's evaluated value. **To evaluate against the entire property value, you must use ^ and $.**

Additionally, you may optionally specify a custom message to be displayed when validation fails: with the `EvaluatedValueFailedValidationMessage` metadata property. If validation fails but this is not specified, the project system will display a default failure message to the user that includes the regular expression.

An example validation is below:

```xml
<StringProperty ...>
    <StringProperty.ValueEditors>
        <ValueEditor EditorType="String">
            <ValueEditor.Metadata>
                <NameValuePair Name="EvaluatedValueValidationRegex" Value="^\d+(\.\d+){0,2}$" />
                <NameValuePair Name="EvaluatedValueFailedValidationMessage" Value="The application display version must be at most a three part version number." /> <!-- optional -->
            </ValueEditor.Metadata>
        </ValueEditor>
    </StringProperty.ValueEditors>
</StringProperty>
```
