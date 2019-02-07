# Feature Comparison

The following is an incomplete list of features differences between the legacy project system and the new project system. 

For a list of behavior differences; see [Compability](compatibility.md).

**Feature**|**Legacy**|**New**|**Notes**
---|:---:|:---:|---
**Platforms**                                                               |
.NET Standard                                                               |          | ●
.NET Core                                                                   |          | ●
.NET Framework                                                              | ●        | ◖  | No ASP.NET AppModel support in new project system
**App Models**                                                              |
ASP.NET Core (.NET Framework & .NET Core)                                   |          | ●
ASP.NET                                                                     | ●        |   
Xamarin                                                                     | ●        |   
Universal Windows Platform (UWP)                                            | ●        |
Windows Presentation Framework (WPF)                                        | ●        | [16.x](https://github.com/dotnet/project-system/labels/Feature-XAML)
Windows Forms                                                               | ●        | [16.x](https://github.com/dotnet/project-system/labels/Feature-WinForms)  
Windows Workflow Foundation (WWF)                                           | ●        |
**Build**|
Target multiple frameworks (multi-target) from single project               |          | ●
Show build (design-time) errors & warnings in Error List as you make them   |          | ●
**Debug**|
Debug multiple frameworks from single project                               |          | ●
Debug with multiple environments from single project ("launch profiles")    |          | ●
Debug settings persistence                                                  |project.csproj.user|launchsettings.json
Modify environment variables on debug                                       |          | ● 
Launch with native debugging                                                | ●        | ◖ | Need to put `"nativeDebugging": true` in launchsettings.json for new project system
Launch with SQL Server debugging                                            | ●        |   
Launch with remote debugging                                                | ●        |   
Launch with Azure Snapshot Debugger                                         |          | ●
**Publish**                                                                 |
Publish to Azure                                                            |          | ●
ClickOnce Publish                                                           | ●        | 
**Project**                                                                 |
Globbing support                                                            |          | ●    | `<Compile Include="*.cs" />`
Simplified project format                                                   |          | ●    | `<Project Sdk="Microsoft.Net.Sdk">`
Simplified configuration syntax                                             |          | ●    | `<Configurations>Debug;Release<Configurations>;<Platforms>AnyCPU;x64</Platforms>`
Implicit configuration syntax                                               | ●        |      | `<PropertyGroup Condition="'$(Configuration)\|$(Platform)' == 'Debug\|AnyCPU'">`
Edit project XML while loaded                                               |          | ●
Find & Find in Files in project file                                        |          | [16.0](https://github.com/dotnet/project-system/issues/4061)
Automatically reload project file with no prompts                           |          | ●
Automatically reload targets files                                          |          | ●
Automatically refresh Solution Explorer to reflect file system              |          | ●
Show items included in imports (.targets/.props)                            |          | ●
**Dependencies**|
Auto-restore packages on load and external edit                             |          | ● 
PackageReference support                                                    | ◖ ● (15.9)        | ● | Starting in 15.9, legacy reloads package targets file without VS restart and supports using MSBuild properties in name, version and metadata.
Dependency node showing package/project graph                               |          | ● 
Transitive ProjectReference                                                 |          | ●
Generate NuGet package on build                                             |          | ● 
**Features**|
Add Service Reference                                                       | ●        | 
Add Web Reference                                                           | ●        | 
Add Data Source                                                             | ●        | 16.x
DataSet Designer                                                            | ●        | 16.x
"Initialize Interactive Window with Project"                                | ●        | ● (15.8) | Only when targeting .NET Framework.
Class Diagrams                                                              | ●        | ● (15.8) |
Code Analysis                                                               | ●        | 
Code Metrics                                                                | ●        | ◖ (15.8) ● (16.0 Preview 1) | 15.8 added support for Code Metrics when targeting .NET Framework. 16.0 will add support for Code Metrics when targeting all frameworks.
Code Clones                                                                 | ●        | ● (15.8) | 
Fakes                                                                       | ●        | 
T4 Templates                                                                | ●        | 
[Automation Extenders](https://msdn.microsoft.com/en-us/library/0y92k2w2.aspx)| ●        | ● (15.8) | 
