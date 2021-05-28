# Visibility Conditions

The Project System may define properties for a project that, in practice, are only visible under certain conditions.

For example, the _Documentation file path_ property only makes sense when the option to generate documentaion files is checked. Similarly, the _Package license expression_ property is only relevant when the _Package license type_ property has a value of _Expression_.

To achieve this, a simple expression language is added which may be evaluated synchronously (and therefore quickly) on the client. This must be fast, as we cannot afford to call back to the server and wait for re-evaluation of the project each time the user toggles a check box, for example. These expressions allow instantaneous feedback in the UI, which feels great to the user.

Visibility conditions are not interpreted by the back-end, which treats them as opaque strings.

## Specification

In a XAML rule file, a visibility condition is specified as metadata on the property. For example:

```xml
<StringProperty ...>
  <StringProperty.Metadata>
    <NameValuePair Name="VisibilityCondition">
      <NameValuePair.Value>(has-evaluated-value "MyPage" "MyProperty" "Foo")</NameValuePair.Value>
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

| Expression             | Value   |
|------------------------|---------|
| `(not true)`           | `false` |
| `(or true false)`      | `true`  |
| `(eq 1 2)`             | `false` |
| `(lt 1 2)`             | `true`  |
| `(add 1 2)`            | `3`     |
| `(add 1 2 3)`          | `6`     |
| `(concat "a" "b")`     | `"ab"`  |
| `(concat "a" "b" "c")` | `"abc"` |

The parentheses envelop a list of values. The evaluation of that list involves treating the first item as a function identifier, and passing the remainder of that list to that function as arguments. You'll notice that some functions accept one argument (`not`), others two arguments (`eq`, `lt`), while others accept an arbitrary number of arguments (`add`, `concat`, `and`, `or`). The number of arguments that a function accepts is its _arity_. Functions that accept an arbitrary number of arguments are known as _variadic_.

These expressions compose nicely. An expression may be an argument to another function. For example `(eq 5 (add 2 3))` is `true`.

To be useful in the context of the Project Properties UI, these expressions must be able to query the state of properties directly. Functions are included for doing so.

The following expression returns the unevaluated value of _MyProperty_ on _MyPage_:

```lisp
(unevaluated "MyPage" "MyProperty")
```

If _MyProperty_ is a boolean property, then this expression stands alone as a visibility condition and will cause the targeted property to only be visible when _MyProperty_ is true. However if _MyProperty_ has non-boolean type then further comparison is required.

For example, if _MyProperty_ is an enum property, the following expression may be used:

```lisp
(eq "MyEnumValue" (unevaluated "MyPage" "MyProperty"))
```

Now, the property will only be visible if _MyProperty_ has value _MyEnumValue_.

## Function Reference

The following table details the default set of visibility expression functions:

| Function                 | Arity    | Description                                                                                     |
|--------------------------|----------|-------------------------------------------------------------------------------------------------|
| `add`                    | Variadic | Adds integer arguments                                                                          |
| `concat`                 | Variadic | Concatenates string arguments                                                                   |
| `eq`                     | 2        | Computes `arg0 == arg1`                                                                         |
| `ne`                     | 2        | Computes `arg0 != arg1`                                                                         |
| `lt`                     | 2        | Computes `arg0 <  arg1`                                                                         |
| `lte`                    | 2        | Computes `arg0 <= arg1`                                                                         |
| `gt`                     | 2        | Computes `arg0 >  arg1`                                                                         |
| `gte`                    | 2        | Computes `arg0 >= arg1`                                                                         |
| `and`                    | Variadic | Computes logical AND of arguments                                                               |
| `or`                     | Variadic | Computes logical OR of arguments                                                                |
| `xor`                    | 2        | Computes exclusive logical OR of arguments                                                      |
| `not`                    | 1        | Computes logical NOT of argument                                                                |
| `unevaluated`            | 2        | Returns the unevaluated value of property on page `arg0` with name `arg1`                       |
| `has-evaluated-value`    | 3        | Returns true if property on page `arg0` with name `arg1` has an evaluated value matching `arg2` |
| `is-codespaces-client`   | 0        | Returns true if the Project Properties UI is running in a Codespaces client                     |
| `has-project-capability` | 1        | Returns true if the project has the specified capability.                                       |
| `has-net-framework`                    | 0 | Returns true if the project targets .NET Framework in at least one configuration. |
| `has-net-core-app`                     | 0 | Returns true if the project targets .NET Core or .NET 5+ in at least one configuration. |
| `has-net-framework-version-or-greater` | 1 | Returns true if the project targets .NET Framework at the specified version or above in at least one configuration. |
| `has-net-core-app-version-or-greater`  | 1 | Returns true if the project targets .NET Core or .NET 5+ at the specified version or above in at least one configuration. |
| `has-csharp-lang-version-or-greater`   | 1 | Returns true if this is a C# project and the language level is `latest`, `preview` or above the specified version. |

These functions are defined in class `VisibilityConditionEvaluator`.

Note that there is no `evaluated` function. A property may have multiple evaluated values, and as such it's not possible to reliably return a single value. Use `has-evaluated-value` instead.

Functions that take a version number should be passed strings containing decimal values. Any leading `v` character is omitted. For example `"v5.0""` and `"1.2.3"` are both valid values.

## Adding Functions

⚠ https://github.com/dotnet/project-system/issues/6895 is tracking the ability to provide additional visibility condition functions &mdash; this documentation will be updated when that issue is resolved.
