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

# Guidelines

## Data
- DO NOT mix snapshot and "live" project data in the same component. 

For example, listening to data flow blocks from `IProjectSubscriptionService` and then reading properties from `ProjectProperties` within the callback will lead to inconsistent results. The dataflow represents a "snapshot" of the project from changes in the past, whereas ProjectProperties represents the actual live project. These will not always agree. The same applies to consuming other CPS APIs from within a dataflow block, the majority of them use live data to provide results and hence will return results inconsistent with the snapshot that you are reading in the dataflow.

- DO NOT parse or attempt to reason about the values of properties that make up the dimensions for a project configuration; `$(Configuration)`, `$(Platform)` and `$(TargetFramework)`, and their plural counterparts; `$(Configurations)`, `$(Platforms)` and `$(TargetFrameworks)`.

These properties are user "aliases" and should only be used for conditions, display and grouping purposes. Instead, the project system should be using their canonical equivalents; `$(PlatformTarget)` instead of `$(Platform)`, and `$(TargetFrameworkMoniker)` instead of `$(TargetFramework)`.

## Threading

- DO follow the [3 threading rules](https://github.com/Microsoft/vs-threading/blob/master/doc/threading_rules.md#3-threading-rules) inside Visual Studio.

- DO NOT call `IProjectThreadingService.ExecuteSynchronously` or `JoinableTaskFactory.Run` from a ThreadPool thread that marshals to another thread (such as via `JoinableTaskFactory.SwitchToMainThreadAsync`).
If you synchronously block on other async code, often that code needs to run or finish on a ThreadPool thread. When the number of threads in the ThreadPool reaches a certain threshold, the ThreadPool manager slows down thread creation and only adds a new thread to the pool every 250 - 500ms. This can result in random UI deadlocks for short periods of time while the code on the UI thread waits for a new thread to be spun up.
