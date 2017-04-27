# Coding Conventions

## Code

We use the same coding style conventions as outlined in [.NET Framework Coding Styles](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md), with the following additions:

- We put one type per file, including nested types. Files containing a nested type, should follow the `Parent.NestedType.cs` convention. Generic types should follow the ``GenericWithOneTypeParameter`1.cs``, ``GenericWithTwoTypeParameters`2.cs`` convention.
- We avoid using regions.
- We sort members in classes in the following order; fields, constructors, events, properties and then methods.
- We favor private fields over private properties

## MEF

- For MEF parts/components, we favor constructor injection over property/field injection.
- We flavor `IVsService<T>` and `IVsService<TService, TInterface>` over usage of `SVsServiceProvider`.

## Tests

- We favor a single Assert per unit test.
- We use the `Method_Setup_Behavior` naming style for unit tests, for example, `GetProperty_NullAsName_ThrowsArgument` or `CalculateValues_WhenDisposed_ReturnsNull`

