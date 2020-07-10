# Opening with the new project system

***NOTE:** The behaviors listed below are subject to change as we add support for more project types in the new project system.*

## When does a project open with the new project system versus the legacy project system?

Because both the new project system and legacy project systems use the same file extensions (csproj, vbproj and fsproj), two factors determine whether a project will open in one or the other.

### TargetFramework/TargetFrameworks properties

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

### SDKs

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

### Project Type GUIDs

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

All three of these projects will be force loaded into the new project system, regardless of the format of the project. This is helpful, for example, if you'd like to move `<TargetFramework>` property to an import.
