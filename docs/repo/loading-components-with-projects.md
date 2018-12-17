## Loading components

Components that are exported through MEF will be automatically discovered as necessary by any imports that exist, but loading and initializing is still a manual process. Additionally, components are typically applied to specific capabilities which can be dynamic, meaning they can be applied or unapplied throughout the lifetime of a project, when:

* A targets file is present, which carries the capability via `<ProjectCapability Include="Foo"/>`
* A package is installed with the capability
* It comes and goes dynamically from other sources

A capability can also be fixed for the lifetime of a project, in which case it cannot be changed without reloading the project. Fixed capabilities tend to come from `[assembly: ProjectTypeRegsitration(Capabilities = "A;B;C")]`.

Why is a "targets file" considered dynamic? Because the outer UnconfiguredProject "inherits" capabilities from any active configuration, targets are only ever evaluated inside a configuration. There is a time early in a project's lifetime before `ProjectLoadCheckpoint.ProjectInitialCapabilitiesEstablished` where there is no active configuration, or the active configuration's capabilities haven't been applied to the UnconfiguredProject. Also, the active configuration can change causing a new set of capabilities to be applied to the UnconfiguredProject.

### The old way

The old way to get your component loaded is to use either the `[ProjectAutoLoad]` or `[ConfiguredProjectAutoLoad]` attributes on a method within your component.

The dynamic nature of capabilities presents a problem however; `[ProjectAutoLoad]` says "load me automatically at the stage I've indicated" but that can be before the set of capabilities you are "applied to" have even been determined. You've said "I need to be loaded before we've determined the active configuration", but your capability doesn't appear until after that.

Another problem is that there's no mechanism to unload the component if the capability disappeared when someone changed the active configuration.

### The new way

To handle these situations, we've decided that changes to capabilities that are applied to `[ProjectAutoLoad]` components, if we've already loaded them or the stage has passed where we should have loaded them, will cause us to automatically [reload the project](https://github.com/Microsoft/VSProjectSystem/blob/master/doc/overview/dynamicCapabilities.md#critical-capabilities-changes-error). We did this for compatibility mostly. We did also consider disposing autoload components if their capability disappeared, but it was very likely that their dependencies probably weren't prepared for that.

To handle these dynamic capabilities that come and go without requiring us to reload the project, we introduced a new concept `IProjectDynamicLoadComponent`; LoadAsync gets called when the capability first appears, UnloadAsync gets called when the capability disappears. Most of our newish components opt into this. This isn't a direct replacement for [ProjectAutoLoad] or [ConfiguredProjectAutoLoad] because components will get loaded later (typically around `ProjectInitialCapabilitiesEstablished`), but is a requirement if you are a dynamic capability.

To help with implementing these dynamic components we also have `AbstractMultiLifetimeComponent` which serves to simplify the lifetime of a component that is loaded and unloaded multiple times.
