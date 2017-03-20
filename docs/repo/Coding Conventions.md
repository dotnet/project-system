### Coding Conventions

### Coding Styles

We use the same coding style conventions as outlined in [.NET Foundation Coding Guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md), with the following additions:

- We put one type per file, including nested types. Files containing a nested type, should follow the `Parent.NestedType.cs` convention. Generic types should follow the `GenericWithOneTypeParameter`1.cs', 'GenericWithTwowTypeParameters`2.cs` convention.
- We do not use regions.
- We sort members in classes in the following order; fields, constructors, events, properties and then methods.

### MEF

- For MEF parts/components, we favor constructor injection over property/field injection.
- We flavor `IVsService<T>` and `IVsService<TService, TInterface>` over usage of `SVsServiceProvider`.
