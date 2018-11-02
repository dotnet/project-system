# Configurations

The way configurations are inferred for a given project is different between the legacy and new project systems. This is also a breaking change in 15.3 compared to 15.0 RTM

## Legacy project system behavior  
In legacy projects configurations of a project are inferred based on conditions in the project file. So if a project had this text,
```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
…
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
…
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' ">
```

the project system would have inferred that the project had two Configurations called Debug and Release and two Platforms called AnyCPU and x86. The old project system would look only in the project and not in any imported props\targets. 

## New project system behavior

### VS 2017 RTM
For the new project system, the old behavior got in the way of project simplification because we would have had to have these conditions in the project file. So for VS2017 RTM we hid these away in the imported targets files and CPS can infer these configurations from there. However, now if the user goes to the configuration manager in VS and deletes\modifies these configurations, we can’t do anything because we’d have to change the imported targets which may not be user files. This was broken in VS2017 RTM.

### VS 2017 "15.3" 
We’ve fixed the issues with deleting the configurations in 15.3 by not inferring these configurations anymore based on conditions but instead we just read two properties called ‘Configurations’ and ‘Platforms’ from the project which would be semi-colon separated list of configurations\platforms. So by default the SDK has these values:

```xml
  <Configurations>Debug;Release</Configurations>
  <Platforms>AnyCPU</Platforms>
```

And these are the same defaults that VS 2017 RTM had. If the user now renames the Debug configuration to MyDebug then we would simply write `<Configurations>MyDebug;Release</Configurations>` to the project file and we get to keep the clean project file.
 
## Breaking change

### TL;DR
If you had a project that had configurations like this:
```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'MyDebug|AnyCPU' " />
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'MyDebug|x86' " />
```

change the project to have two properties like this:
```xml
<PropertyGroup>
   <Configurations>MyDebug;Debug;Release</Configurations>
   <Platforms>AnyCPU;x86</Platforms>
</PropertyGroup>
```

### Details

This is a breaking change because we *only* read configurations from these properties now and don't infer them anymore from conditions on propertygroups. If someone had created a new configuration with RTM tools like this
```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'MyDebug|AnyCPU' ">
```
and that was set as the active configuration then the active configuration gets persisted into the sln file. The RTM product would have inferred the MyDebug config and loaded it whereas 15.3 looks at the Configurations property and will think it only has Debug. The sln file will however ask the project system to load the MyDebug configuration which will cause the project to fail to load with a configuration not found error.
 
### Rationale
We looked at the telemetry we have for how many .NET Core projects were created with non-standard configurations and it was about 1% of projects and very small absolute number as well. The effort to have a hybrid and support both styles of inferring configurations would be quite high and based on the data we don't think it's worth investing in given the fix for the affected projects is fairly straightforward. 
