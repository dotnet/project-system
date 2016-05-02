### Code

We use the same coding style conventions as outlined in [.NET Foundation Coding Guidelines](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md), with the following additions
- Put one type per file, including nested types. Files containing a nested type, should follow the `Parent.NestedType.cs` convention
- We do not use regions
- For MEF parts/components, we favor constructor injection over property/field injection
- We sort members in classes in the following order; fields, constructors, events, properties and then methods
