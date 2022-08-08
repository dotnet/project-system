# Visibility and other Property Conditions

For example, the Project System may define properties for a project that, in practice, are only visible under certain conditions.

For instance, the _Documentation file path_ property only makes sense when the option to generate documentaion files is checked. Similarly, the _Package license expression_ property is only relevant when the _Package license type_ property has a value of _Expression_.

To achieve this, a simple expression language is added which may be evaluated synchronously (and therefore quickly) on the client. This must be fast, as we cannot afford to call back to the server and wait for re-evaluation of the project each time the user toggles a check box, for example. These expressions allow instantaneous feedback in the UI, which feels great to the user.

However, visibility conditions are not the only conditions that properties may use. For example, properties that are to be read-only only under certain conditions may define an _IsReadOnlyCondition_ to control this behavior.
Thus these evaluated conditions together are "property conditions" that are not interpreted by the back-end, which treats them as opaque strings.

## Specification

There are four kinds of property conditions:
- VisibilityCondition is a visibility condition on a property (whether the property should be shown).
- DimensionVisibilityCondition is a visibility condition on a dimension name (whether users should be allowed to select to vary/unvary by the dimension.)
- ConfiguredValueVisibilityCondition is a visibility condition on a PropertyValue (whether the value for each individual dimensional configuration should be shown). For example, if a property varies by dimension but the value only applies to iOS, other targets can be hidden.
- IsReadOnlyCondition is a condition on a property that determines whether the property should be editable (if true or not specified) or readonly (if evaluated to false).

In a XAML rule file, a property condition is specified as metadata on the property. For example:

```xml
<StringProperty ...>
  <StringProperty.Metadata>
    <NameValuePair Name="VisibilityCondition">
      <NameValuePair.Value>(has-evaluated-value "MyPage" "MyProperty" "Foo")</NameValuePair.Value>
    </NameValuePair>
    <NameValuePair Name="DimensionVisibilityCondition">
        <NameValuePair.Value>(matches "Configuration" (dimension))</NameValuePair.Value> <!-- '(dimension)' refers to each dimension candidate -->
    </NameValuePair>
    <NameValuePair Name="ConfiguredValueVisibilityCondition">
        <NameValuePair.Value>(eq (evaluated "ConfigurationGeneralPage" "TargetPlatformIdentifier") "Android")</NameValuePair.Value> <!-- 'evaluated' function is allowed here -->
    </NameValuePair>
    <NameValuePair Name="EditabilityCondition">
        <NameValuePair.Value>
            (not (has-evaluated-value "Build" "WarningSeverity" "DisableAll"))
        </NameValuePair.Value>
    </NameValuePair>
  </StringProperty.Metadata>
</StringProperty>
```

For more information, see [Property Specification](property-specification.md).

## Syntax

The syntax uses Lisp-style [S-expressions](https://en.wikipedia.org/wiki/S-expression).

By way of example, the simplest expression is a literal value.

| Expression | Value     |
|------------|-----------|
| `"hello"`  | `"hello"` |
| `1`        | `1`       |
| `true`     | `true`    |

To make this language useful, we need the ability to perform operations with these values. Here are some examples:

| Expression             | Value             |
|------------------------|-------------------|
| `(not true)`           | `false`           |
| `(or true false)`      | `true`            |
| `(eq 1 2)`             | `false`           |
| `(lt 1 2)`             | `true`            |
| `(add 1 2)`            | `3`               |
| `(add 1 2 3)`          | `6`               |
| `(concat "a" "b")`     | `"ab"`            |
| `(concat "a" "b" "c")` | `"abc"`           |

The parentheses envelop a list of values. The evaluation of that list involves treating the first item as a function identifier, and passing the remainder of that list to that function as arguments. You'll notice that some functions accept one argument (`not`), others two arguments (`eq`, `lt`), while others accept an arbitrary number of arguments (`add`, `concat`, `and`, `or`). The number of arguments that a function accepts is its _arity_. Functions that accept an arbitrary number of arguments are known as _variadic_.

These expressions compose nicely. An expression may be an argument to another function. For example `(eq 5 (add 2 3))` is `true`.

To be useful in the context of the Project Properties UI, these expressions must be able to query the state of properties directly. Functions are included for doing so.

The following expression returns the unevaluated value of _MyProperty_ on _MyPage_:

```lisp
(unevaluated "MyPage" "MyProperty")
```

If _MyProperty_ is a boolean property, then this expression stands alone as a property condition and will cause the targeted property to only be visible when _MyProperty_ is true. However if _MyProperty_ has non-boolean type then further comparison is required.

For example, if _MyProperty_ is an enum property, the following expression may be used:

```lisp
(eq "MyEnumValue" (unevaluated "MyPage" "MyProperty"))
```

Now, the property will only be visible if _MyProperty_ has value _MyEnumValue_.

## Function Reference

The following table details the default set of property condition expression functions. These are the only functions allowed in both `VisibilityCondition`s and `IsReadOnlyCondition`s

| Function                               | Arity    | Description                                                                                                              |
|----------------------------------------|----------|--------------------------------------------------------------------------------------------------------------------------|
| `add`                                  | Variadic | Adds integer arguments                                                                                                   |
| `concat`                               | Variadic | Concatenates string arguments                                                                                            |
| `eq`                                   | 2        | Computes `arg0 == arg1`                                                                                                  |
| `ne`                                   | 2        | Computes `arg0 != arg1`                                                                                                  |
| `lt`                                   | 2        | Computes `arg0 <  arg1`                                                                                                  |
| `lte`                                  | 2        | Computes `arg0 <= arg1`                                                                                                  |
| `gt`                                   | 2        | Computes `arg0 >  arg1`                                                                                                  |
| `gte`                                  | 2        | Computes `arg0 >= arg1`                                                                                                  |
| `and`                                  | Variadic | Computes logical AND of arguments                                                                                        |
| `or`                                   | Variadic | Computes logical OR of arguments                                                                                         |
| `xor`                                  | 2        | Computes exclusive logical OR of arguments                                                                               |
| `or`                                   | Variadic | Computes logical OR of arguments                                                                                         |
| `not`                                  | 1        | Computes logical NOT of argument                                                                                         |
| `matches`                              | 2        | Returns whether the regular expression defined as the second parameter matches the first parameter, which is a string    |
| `if`                                   | 3        | Evaluates the first parameter. If it is true, returns the second parameter, otherwise returns the third parameter        |
| `unevaluated`                          | 2        | Returns the unevaluated value of property on page `arg0` with name `arg1`                                                |
| `is-codespaces-client`                 | 0        | Returns true if the Project Properties UI is running in a Codespaces client                                              |
| `has-project-capability`               | 1        | Returns true if the project has the specified capability.                                                                |
| `is-csharp`                            | 0        | Returns true if this is a C# project.                                                                                    |
| `is-vb`                                | 0        | Returns true if this is a VB project.                                                                                    |
| `has-vb-lang-version-or-greater`       | 1        | Returns true if this is a VB project and the language level is `latest`, `preview` or above the specified version.       |
| `has-platform`                         | 1        | Returns true if the project's target platform matches. Examples are `windows`, `android`, `ios`.                         |
| `has-net-framework`                    | 0        | Returns true if the project targets .NET Framework in at least one configuration.                                        |
| `has-net-core-app`                     | 0        | Returns true if the project targets .NET Core or .NET 5+ in at least one configuration.                                  |
| `has-net-framework-version-or-greater` | 1        | Returns true if the project targets .NET Framework at the specified version or above in at least one configuration.      |
| `has-net-core-app-version-or-greater`  | 1        | Returns true if the project targets .NET Core or .NET 5+ at the specified version or above in at least one configuration |
| `has-csharp-lang-version-or-greater`   | 1        | Returns true if this is a C# project and the language level is `latest`, `preview` or above the specified version.       |
| `has-evaluated-value`                  | 3        | Returns true if property on page `arg0` with name `arg1` has an evaluated value matching `arg2`                          |

These functions are defined in class `BasePropertyConditionEvaluator`.

#### Functions only available for `DimensionVisibilityCondition`
These functions are defined in the `DimensionVisibilityConditionEvaluator` class

| Function    | Arity    | Description                                   |
|-------------|----------|-----------------------------------------------|
| `dimension` | 0        | Returns the current dimension being evaluated |

#### Functions only available for `ConfiguredValueVisibilityCondition`
These functions are defined in the `ConfiguredVisibilityConditionEvaluator` class

| Function               | Arity | Description                                                              |
|------------------------|-------|--------------------------------------------------------------------------|
| `evaluated`            | 2     | Returns the evaluated value of property on page `arg0` with name `arg1`. |
| `has-evaluated-values` | 0     | Returns whether this property value contains nested aggregated value(s). |

Note that the `evaluated` function is not available in the VisibilityCondition condition, as a Property may have multiple evaluated values, and as such it's not possible to reliably return a single value. Use `has-evaluated-value` instead in this case.

However, it is available in a ConfiguredValueVisibilityCondition expression, as these are run on each PropertyValue, in which there will be only one possible evaluated value for a property.

Functions that take a version number should be passed strings containing decimal values. Any leading `v` character is omitted. For example `"v5.0""` and `"1.2.3"` are both valid values.

## Adding Functions

⚠ https://github.com/dotnet/project-system/issues/6895 is tracking the ability to provide additional property condition functions &mdash; this documentation will be updated when that issue is resolved.
