# Localization

This document contains background information and guidance on how to provide localized Property Page and Launch Profile UIs.

## Background

The base layer of the project system, called the Common Project System or just CPS, is responsible for finding and loading .xaml `Rule` files. It does this via two mechanisms:

- Enumerating all the `PropertyPageSchema` MSBuild items in your project or (more likely) the imported .props and .targets files.
- Examining the MEF metadata provided by `ExportPropertyXamlRuleDefinitionAttribute`s and using that to locate .xaml files embedded in assemblies as resources.

These CPS mechanisms do not provide their own means of handling localized `Rule`s, and so we need to handle it in the lower layers (MSBuild and resource loading).

## A note on creating localized XAML files

Both of these approaches assume that you have localized copies of the original .xaml `Rule` file, one per supported locale. The process of creating these files is beyond the scope of this document, and will depend on the specific localization tools and processes used by your component.

That being said, this particular repo makes use of [xliff-tasks](https://github.com/dotnet/xliff-tasks) to extract the localizable parts of .xaml `Rule` files into .xlf files for translation, and to incorporate the translated .xlf files back into the build.


## Integrating localized XAML files on disk

Say you start with the following directory structure containing your .props and .targets files, and your unlocalized `Rule` file:

```
MyProject
  +- MyProject.props
  +- MyProject.targets
  +- Rules
     +- MyRule.xaml
```

With the corresponding `PropertyPageSchema` item in MyProject.targets:

``` xml
<PropertyPageSchema Include="$(MSBuildThisFileDirectory)Rules\MyRule.xaml">
  <Context>Project</Context>
</PropertyPageSchema>
```

In this approach, the localized .xaml files go in locale-specific sub-directores:

```
MyProject
  +- MyProject.props
  +- MyProject.targets
  +- Rules
     +- MyRule.xaml
     +- es
        +- MyRule.xaml
     +- fr
        +- MyRule.xaml
     +- ja
        +- MyRule.xaml
     +- pt-BR
        +- MyRule.xaml
```

We then use some MSBuild logic to determine which locale-specific sub-directory to look in:

``` xml
<!--
  Rule files that don't need localization go in the neutral directory to save duplicating files into each language
-->
<PropertyGroup Condition="'$(NeutralResourcesDirectory)' == ''">
  <NeutralResourcesDirectory>$(MSBuildThisFileDirectory)Rules</NeutralResourcesDirectory>
</PropertyGroup>

<!--
  Locate the approriate localized xaml resources based on the language ID or name.

  The logic here matches the resource manager sufficiently to handle the fixed set of 
  possible VS languages and directories on disk.

  We cannot respect the exact probe order of the Resource Manager as this has to evaluate statically
  and we have only LangName and LangID and no access to System.Globalization API.
-->
<PropertyGroup Condition="'$(ResourcesDirectory)' == ''">
  <!-- 1. Probe for exact match against LangName. (e.g. pt-BR) -->
  <ResourcesDirectory>$(MSBuildThisFileDirectory)Rules\$(LangName)</ResourcesDirectory>

  <!-- 2. Handle special cases of languages which would not match above or below. -->
  <ResourcesDirectory Condition="!Exists('$(ResourcesDirectory)') and '$(LangID)' == '2052'">$(MSBuildThisFileDirectory)Rules\zh-Hans</ResourcesDirectory>
  <ResourcesDirectory Condition="!Exists('$(ResourcesDirectory)') and '$(LangID)' == '1028'">$(MSBuildThisFileDirectory)Rules\zh-Hant</ResourcesDirectory>

  <!-- 3. Probe for parent by taking portion the portion before the hyphen (e.g. fr-FR -> fr) -->
  <ResourcesDirectory Condition="!Exists('$(ResourcesDirectory)')">$(MSBuildThisFileDirectory)Rules\$(LangName.Split('-')[0])</ResourcesDirectory>

  <!-- 4. Fall back to neutral resources if all of the above fail -->
  <ResourcesDirectory Condition="!Exists('$(ResourcesDirectory)')">$(NeutralResourcesDirectory)</ResourcesDirectory>
</PropertyGroup>

<PropertyGroup>
  <!-- Ensure a trailing slash -->
  <ResourcesDirectory Condition="!HasTrailingSlash('$(ResourcesDirectory)')">$(ResourcesDirectory)\</ResourcesDirectory>
</PropertyGroup>
```

This sets `$(ResourcesDirectory)` to the appropriate locale-specific directory based on the `$(LangName)` and `$(LangID)` properties supplied by MSBuild.

The `PropertyPageSchema` items also need to be updated accordingly:

``` xml
<PropertyPageSchema Include="$(ResourcesDirectory)MyRule.xaml">
  <Context>Project</Context>
</PropertyPageSchema>
```

## Integrating embedded localized XAML

_To be written_

