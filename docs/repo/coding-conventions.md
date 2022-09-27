# Coding Conventions

## Code

We use the same coding style conventions as outlined in [.NET Framework Coding Styles](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md), with the following additions:

- **DO** put one type per file, including nested types. Files containing a nested type, should follow the `Parent.NestedType.cs` convention. Generic types should follow the ``GenericWithOneTypeParameter`1.cs``, ``GenericWithTwoTypeParameters`2.cs`` convention. If you have a single generic type,`GeneraticWithOneTypeParameter.cs` is acceptable.
- **DO NOT** use regions.
- **DO** sort members in classes in the following order; fields, constructors, events, properties and then methods.
- **DO** favor private fields over private properties.
- **DO** case internal fields as `PascalCased` not `_camelCased`.

The majority of the guidelines, where possible, are enforced via the [.editorconfig](/.editorconfig) in the root the repository.

## MEF

- **DO** use constructor injection over property/field injection.
  
- **DO** use MEF imports over direct usage of `IComponentModel`.

## VS APIs

- **DO** favor `IVsUIService<T>` and `IVsUIService<TService, TInterface>` over usage of `IServiceProvider`.
  
   `IVsUIService` enforces UI thread access which prevents accidental RPC calls from a background thread.
  
- **DO** favor `IVsService<T>` and `IVsService<TService, TInterface>` over usage of `IAsyncServiceProvider`.
  
   `IVsService` ensures casts are performed on the UI thread which prevents accidental RPC calls from a background thread.

- **DO** favor `HResult` over `VSConstants` and raw constants.

- **DO** favor `HierarchyId` over `VSConstants.VSITEMID` and raw constants.

## Tests

- **DO** favor a single Assert per unit test.

- **DO** use the `Method_Setup_Behavior` naming style for unit tests, for example, `GetProperty_NullAsName_ThrowsArgument` or `CalculateValues_WhenDisposed_ReturnsNull`.

- **DO** favor static `CreateInstance` for creating the object under test versus directly calling the constructor

   This reduces the amount of refactoring/fixup needed when adding a new import to a service.

# Guidelines

## Data

- **DO NOT** mix snapshot and "live" project data in the same component. 

   For example, listening to data flow blocks from `IProjectSubscriptionService` and then reading properties from `ProjectProperties` within the callback will lead to inconsistent results. The dataflow represents a "snapshot" of the project from changes in the past, whereas ProjectProperties represents the actual live project. These will not always agree. The same applies to consuming other CPS APIs from within a dataflow block, the majority of them use live data to provide results and hence will return results inconsistent with the snapshot that you are reading in the dataflow.

- **DO NOT** parse or attempt to reason about the values of properties that make up the dimensions for a project configuration; `$(Configuration)`, `$(Platform)` and `$(TargetFramework)`, and their plural counterparts; `$(Configurations)`, `$(Platforms)` and `$(TargetFrameworks)`.

   These properties are user "aliases" and should only be used for conditions, display and grouping purposes. Instead, the project system should be using their canonical equivalents; `$(PlatformTarget)` instead of `$(Platform)`, and `$(TargetFrameworkMoniker)` and `$(TargetPlatformMoniker)` instead of `$(TargetFramework)`

## Threading

- **DO** follow the [3 threading rules](https://github.com/Microsoft/vs-threading/blob/master/doc/threading_rules.md#3-threading-rules) inside Visual Studio.

- **DO NOT** call `IProjectThreadingService.ExecuteSynchronously` or `JoinableTaskFactory.Run` from a ThreadPool thread that marshals to another thread (such as via `JoinableTaskFactory.SwitchToMainThreadAsync` or calling an STA-based `IVsXXX` object).

   If you synchronously block on other async code, often that code needs to run or finish on a ThreadPool thread. When the number of threads in the ThreadPool reaches a certain threshold, the ThreadPool manager slows down thread creation and only adds a new thread to the pool every 250 - 500ms. This can result in random UI deadlocks for short periods of time while the code on the UI thread waits for a new thread to be spun up. See [ThreadPool Starvation](https://github.com/Microsoft/vs-threading/blob/master/doc/threadpool_starvation.md) for more information.

- **AVOID** marking `await` calls with `ConfigureAwait(true)` or `ConfigureAwait(false)`.

   We follow the [Visual Studio guidelines](https://github.com/Microsoft/vs-threading/blob/master/doc/cookbook_vs.md#should-i-await-a-task-with-configureawaitfalse) around `ConfigureAwait` usage.
