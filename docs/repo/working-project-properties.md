
# Project Configuration Properties & Project Properties

In the following discussion, I am using C# as a reference but this discussion holds true for VB as well.

In General, we have 2 ways to get the Project Configuration Properties. One is through the Project IVSHierarchy’s GetProperty using IVsCfgBrowseObject and other is through Automation, via DTE.


## Working of the native/legacy project system

[Pic]

In the legacy project system, both of these mechanisms end up returning the same object, CCSharpProjectConfigProperties, which implements IVsCfgBrowseObject and a bunch of public interfaces like CSharpProjectConfigurationProperties3, CSharpProjectConfigurationProperties4, CSharpProjectConfigurationProperties5, CSharpProjectConfigurationProperties6 and few more. 

## Working of the new  CPS based project system - Current state

[Pic]

In the CPS world, the object returned through IVsCfgBrowseObject implementation(ProjectConfig object) and the object returned via Automation(ProjectConfigProperties whose only property is OutputPath) are different. This behavior breaks back-compat because the Automation object no longer implements the public VS interfaces mentioned earlier. Hence, to support back-compat, the Managed project system exports an implementation, CSharpProjectConfigurationProperties, of these interfaces which replaces the default automation object.

## Future design of the new CPS based project system

[Pic]

We would like to get to a design, where Managed project system exports an implementation of the public VS interfaces, which gets ComAggregated over ProjectConfig. This ProjectConfig will then be imported by a ProjectConfigWrapper, which then exports the ProjectConfig instance as the Automation Object. With this design, the same instance of the ProjectConfig, which implements the public VS interfaces, will be provided for both the approaches(DTE and IVsCfgBrowseObject). This behavior is similar to the native project system behavior and restores backward compatibility.

## Project Properties

Project properties are not configuration based. In the legacy project system, CCSharpProjectProperties object is returned through IVsHierarchy’s GetProperty and through Automation(ENVDTE.Project.Properties). In the new project system, we return DynamicTypeBrowseObject, backed by the browse object rule when the IVsHierarchy’s GetProperty or the Automation call is made. CCSharpProjectProperties, similar to CCSharpProjectConfigProperties, implements a bunch of public VS interfaces(CSharpProjectProperties3, CSharpProjectProperties4, CSharpProjectProperties5…), which are not implemented by the DynamicTypeBrowseObject object. This is not a problem as long as the browse object contains the properties that the interfaces defined because EnvDte.Project.Properties does not return CCSharpProjectProperties, which implements the VS interface, but instead returns a wrapper around the implementation, although the interfaces were public.