# Feature Comparison

The following is an incomplete list of features differences between the legacy project system and the new project system. 

For a list of behavior differences; see [Compatibility](compatibility.md).

**Feature**|**Legacy**|**New**|**Notes**
---|:---:|:---:|---
**Platforms**                                                               |
.NET Standard                                                               |          | ●
.NET Core                                                                   |          | ●  | Includes .NET 5.0 and later
.NET Framework                                                              | ●        | ◖  | No ASP.NET AppModel support in new project system
**App Models**                                                              |
ASP.NET Core (.NET Framework & .NET Core)                                   |          | ●
ASP.NET                                                                     | ●        |   
Xamarin                                                                     | ●        | ● (17.0)
Universal Windows Platform (UWP)                                            | ●        |
Windows Presentation Framework (WPF)                                        | ●        | ● (16.3)
Windows Forms                                                               | ●        | ● (16.3)
Windows Workflow Foundation (WWF)                                           | ●        |
**Build**|
Target multiple frameworks (multi-target) from single project               |          | ●
Show build (design-time) errors & warnings in Error List as you make them   |          | ●
**Debug/Run**                                                               |
Debug multiple frameworks from single project                               |          | ●
Debug with multiple environments from single project ("launch profiles")    |          | ●
Debug settings persistence                                                  |project.csproj.user|launchsettings.json
Influence environment variables on debug                                    |          | ● 
Launch with native debugging                                                | ●        | ●
Launch with SQL Server debugging                                            | ●        | ● (16.4)
Launch with remote debugging                                                | ●        | ● (16.5)
Launch with Azure Snapshot Debugger                                         |          | ●
Hot Reload                                                                  |          | ● (17.0)
**Publish**                                                                 |
Publish to Azure                                                            |          | ●
ClickOnce Publish                                                           | ●        | 
**Project**                                                                 |
Globbing support                                                            |          | ●    | `<Compile Include="*.cs" />`
Simplified project format                                                   |          | ●    | `<Project Sdk="Microsoft.Net.Sdk">`
Simplified configuration syntax                                             |          | ●    | `<Configurations>Debug;Release<Configurations>;<Platforms>AnyCPU;x64</Platforms>`
Implicit configuration syntax                                               | ●        |      | `<PropertyGroup Condition="'$(Configuration)\|$(Platform)' == 'Debug\|AnyCPU'">`
Edit project XML while project is loaded                                    |          | ●
Find & Find in Files in project file                                        |          | ●
Automatically reload project file with no prompts                           |          | ●
Automatically reload targets files                                          |          | ●
Automatically refresh Solution Explorer to reflect file system              |          | ●
Show items included in imports (.targets/.props)                            |          | ●
**Dependencies**|
Auto-restore packages on load and external edit                             |          | ● 
Packages.config support                                                     | ●        |
PackageReference support                                                    | ●        | ●
Dependency node showing package/project graph                               |          | ● 
Transitive ProjectReference                                                 |          | ●
Generate NuGet package on build                                             |          | ● 
**Features**|
Add Service Reference                                                       | ●        | 
Add Web Reference                                                           | ●        | 
Add Data Source                                                             | ●        | ● (16.4)
Settings Designer                                                           | ●        | ● | Added support for .NET Core 3.0 in 16.7
DataSet Designer                                                            | ●        | ●
"Initialize Interactive Window with Project"                                | ●        | ● | Only when targeting .NET Framework.
Class Diagrams                                                              | ●        | ●
Code Analysis                                                               | ●        | 
Code Metrics                                                                | ●        | ● 
Code Clones                                                                 | ●        | ●
Fakes                                                                       | ●        | ● (16.7)
T4 Templates                                                                | ●        | 
[Automation Extenders](https://docs.microsoft.com/previous-versions/0y92k2w2(v=vs.140))| ●      | ●
