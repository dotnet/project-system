# Project Configuration Properties & Project Properties

In the following discussion, C# is used as a reference but this discussion holds true for VB as well.

In general, we have two ways to get _Project Configuration Properties_. One is through the project's `IVSHierarchy.GetProperty` method using `IVsCfgBrowseObject`. The other is through Automation via DTE.


## Legacy project system

![native](https://cloud.githubusercontent.com/assets/10550513/26130958/0df90f52-3a4c-11e7-8fd9-a3c50198148f.png)

In the legacy project system, both of these mechanisms end up returning the same object, `CCSharpProjectConfigProperties`, which implements `IVsCfgBrowseObject` and a bunch of public interfaces like `CSharpProjectConfigurationProperties3`, `CSharpProjectConfigurationProperties4`, `CSharpProjectConfigurationProperties5`, `CSharpProjectConfigurationProperties6` and few more.

## CPS-based project system

![current](https://cloud.githubusercontent.com/assets/10550513/26237643/dfc6113a-3c2a-11e7-87cc-c45acb42a6ff.png)

In the CPS world, the object returned through `IVsCfgBrowseObject` implementation (`ProjectConfig` object) and the object returned via Automation are different. This automation object, `CSharpProjectConfigurationProperties`, exported by Managed Project System, overrides the default implementation.

## Future design for the new CPS based project system

![future](https://cloud.githubusercontent.com/assets/10550513/26237655/ed3cb60c-3c2a-11e7-923f-9908ddc641a4.png)

In the future, we would like to get to a design similar to the legacy project system, where the browse object and the automation object are both the same.

To achieve this,

1. Managed project system will export an implementation of the public VS interfaces, which gets ComAggregated over ProjectConfig.
2. This `ProjectConfig` will then be imported by Some_Wrapper, which then exports the `ProjectConfig` instance as the Automation Object.

## Project Properties

Project properties are not configuration based. In the legacy project system, `CCSharpProjectProperties` object is returned through `IVsHierarchy.GetProperty` and through Automation (`ENVDTE.Project.Properties`). In the new project system, we return `DynamicTypeBrowseObject`, backed by the browse object rule when `IVsHierarchy.GetProperty` or Automation is called. `CCSharpProjectProperties`, similar to `CCSharpProjectConfigProperties`, implements a bunch of public VS interfaces (`CSharpProjectProperties3`, `CSharpProjectProperties4`, `CSharpProjectProperties5`...), which are not implemented by the `DynamicTypeBrowseObject` object. This is not a problem as long as the browse object contains the properties that the interfaces defined because `EnvDte.Project.Properties` does not return `CCSharpProjectProperties`, which implements the VS interface, but instead returns a wrapper around the implementation, although the interfaces were public.
