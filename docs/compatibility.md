# Compatibility

The following is a list of known compatibility issues and behavioral differences between the legacy project system and the new project system.

For a list of feature differences; see [Feature Comparison](feature-comparison.md).

## Builds

### Design-time builds are run out-of-process.
Similar to normal builds, the new project system runs [design-time builds](design-time-builds.md) in a separate process instead of within the Visual Studio process. This means that tasks and assemblies adhere to the binding policy of MSBuild.exe regardless of whether they loaded in a design-time build or a normal build. In the legacy project system, design-time builds use the binding policy of Visual Studio (devenv.exe), whereas normal builds use the binding policy of MSBuild.exe.

### Design-time builds are asynchronous.
The legacy project system used to guarantee that a design-time build had occurred by the time certain changes had been done to the project, such as adding or removing files or switching configurations. While easier for components to reason about, this was to the detriment of user experience because this would be done as a UI blocking call.

In the new project system design-time builds are asynchronous, and are not guaranteed to have completed by the time the above changes have been made to the project.

### Design-time build errors and warnings show in the Error List
Design-time build errors and warnings appear in the Error List alongside a normal build's errors and warnings. This might result in warnings and errors showing up that we're previously hidden by the legacy project system.

### Design-time builds might run targets in the same build
For performance reasons, the new project system will group and run multiple targets together in the same build which might result in different behavior for targets that have incomplete or inaccurate target dependencies.

## Configurations

### Configurations are inferred differently
To keep the project file simple, configurations are inferred differently. More details [here](configurations.md)
