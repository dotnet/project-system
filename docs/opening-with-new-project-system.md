# Opening with the new project system

## Background

The solution (.sln) file specifies which project system should be used to open each project. For example:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Library3", "Library3.csproj", "{ADFEAAF5-225C-4E13-8B65-77057AAC44B8}"
EndProject
```

Here the "Library3.csproj" project should be opened with the project system designated by the "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC" GUID.

While the solution file entry associates a specific project with a specific project system, ultimately this information comes from the [ProviderProjectFactoryAttribute](https://learn.microsoft.com/dotnet/api/microsoft.visualstudio.shell.provideprojectfactoryattribute)--or the equivalent entries in a .pkgdef file--which associates a file extension (like .csproj) with a project system.

## Problem

There may be situations where more than one project system is capable of loading project files with a particular extension, or where the choice of project system needs to be dynamically determined. In this case the solution file itself does not contain enough information for the correct project system to be determined.

## General Solution

One option is to provide some sort of upgrade tool that can analyze the solution file and project, recommend the user switch that project to a different project system, and then make the required changes to the solution file on their behalf. However, this requires interactions with the user (which may become noisy if there are a lot of projects to upgrade) changes to the solution file (which the user may not understand) and reloading the project. This is also not a particularly dynamic approach as the change to the project system is persisted in the solution file.

To better address the situation where you may need to dynamically choose the project system, VS provides the [IVSProjectSelector](https://learn.microsoft.com/dotnet/api/microsoft.visualstudio.shell.interop.ivsprojectselector) interface. Implementations of this interface are associated with a particular project system, and are called before a project is loaded in order to redirect the load to a different project system.

When an IVSProjectSelector is in play, the following sequence of events occurs before a project is loaded:

1. The solution loader reads the project system GUID specified in the .sln file.
2. If an implementation of IVSProjectSelector has been associated with that project system GUID it is invoked with the following information:
    1. The original project system GUID
    2. The path to the project file
3. The selector can respond in one of two ways:
    1. Decline the request, in which case the project will be loaded with the project system in the .sln file.
    2. Return the GUID of a different project system, in which case it will be used to load the project. 

## C#/VB/F# Specifics

### Our IVSProjectSelector implementation

The implementation of our IVSProjectSelector lives in the VS repo as it is associated with the older CSProj project system. However, the registry keys that actually associated the selector with the CSProj project system can be found in [ProjectSelectors.pkgdef](https://github.com/dotnet/project-system/blob/1aa6689827ba43e8cd7b9d29a6d15b3eabf6842c/setup/ProjectSystemSetup/ProjectSelectors.pkgdef); we only want the selector to be active when this project system is installed.

***NOTE:** The behaviors listed below are subject to change as we add support for more project types in the new project system.*

### When does a project open with the new project system versus the legacy project system?

Because both the new project system and legacy project systems use the same file extensions (csproj, vbproj and fsproj), two factors determine whether a project will open in one or the other.

#### TargetFramework/TargetFrameworks properties

*Applies to C# and Visual Basic only*

If a csproj or vbproj project contains a `<TargetFramework>` or `<TargetFrameworks>` property in the body of the project file (not in any of its imports), then it will be automatically opened in the new project system. Specifically, **in version 16.3 and earlier** Visual Studio will scan the raw text of the project file for `</TargetFramework>` or `</TargetFrameworks>`. In **version 16.4 and later** Visual Studio will look for a `<TargetFramework>` or `<TargetFrameworks>` element parented by a `<PropertyGroup>` element.

For example, the following two csproj or vbproj projects will open in the new project system:

``` XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net45</TargetFramework>
  </PropertyGroup>
</Project>
```

``` XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net45;netstandard1.3</TargetFrameworks>
  </PropertyGroup>
</Project>
```

Whereas, the following csproj or vbproj will open in the legacy project system:

``` XML
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />

  <PropertyGroup>
      <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
  </PropertyGroup>

  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

#### SDKs

*Applies to F# in 16.3 and earlier, and to F#, C#, and Visual Basic in 16.4 and later*

If a project is marked as importing an SDK in the body of the project file (not in any of its imports), then the project is opened in the new project system.

Specifically, VS looks for an `Sdk` attribute within a `<Project>` or `<Import>` element. For example, the following two projects will open in the new project system:

``` XML
<Project Sdk="Microsoft.NET.Sdk">

</Project>
```

``` XML
<Project>
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />

  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
</Project>
```

In addition, for C# and Visual Basic projects only, VS will look for an `<Sdk>` element parented by a `<Project>` element. For example:

``` XML
<Project>
  <Sdk Name="Microsoft.NET.Sdk" Version="1.2.3" />

</Project>
```

Whereas the following will open in the legacy project system:

``` XML
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />

  <PropertyGroup>
      <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
  </PropertyGroup>

  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

#### Project Type GUIDs

Inside the solution, there are GUIDs associated with a project called a "project type". By default, all csproj, vbproj and fsproj point to the following three GUIDs (the first GUID in the line):

```
Project("{F2A71F9B-5D33-465A-A702-920D77279786}") = "Library1", "Library1.fsproj", "{9B232C4C-AE37-4BC6-A68A-52A275F253C2}"
EndProject
Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "Library2", "Library2.vbproj", "{629B0BD5-ADD4-46A9-85E2-0D75CA49DCCB}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Library3", "Library3.csproj", "{ADFEAAF5-225C-4E13-8B65-77057AAC44B8}"
EndProject
```

When these GUIDs are set, the behavior called out above of whether to open in the new project system or the old project system kicks in. However, it is possible to force projects to open in the new project system by changing these GUIDs to the following:

```
Project("{6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705}") = "Library1", "Library1.fsproj", "{9B232C4C-AE37-4BC6-A68A-52A275F253C2}"
EndProject
Project("{778DAE3C-4631-46EA-AA77-85C1314464D9}") = "Library2", "Library2.vbproj", "{629B0BD5-ADD4-46A9-85E2-0D75CA49DCCB}"
EndProject
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Library3", "Library3.csproj", "{ADFEAAF5-225C-4E13-8B65-77057AAC44B8}"
EndProject
```

All three of these projects will be force loaded into the new project system, regardless of the format of the project. This is necessary only in the rare case that your project has none of the items described above.

If you're using the newer `.slnx` format for your solution, you can use the `Type` attribute of the  `Project` element to achieve the same result:

```xml
<Solution>
  <Project Path="Library1.fsproj" Type="6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705" />
  <Project Path="Library2.vbproj" Type="778DAE3C-4631-46EA-AA77-85C1314464D9" />
  <Project Path="Library3.csproj" Type="9A19103F-16F7-4668-BE54-9A1E7A4F7556" />
</Solution>
```
