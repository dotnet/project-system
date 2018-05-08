# Feature Comparison

The following is an incomplete list of features differences between the legacy project system and the new project system. 

For a list of behavior differences; see [Compability](compatibility.md).

**Feature**|**Legacy**|**New**|**Notes**
---|:---:|:---:|---
**Platforms**                                                               |
.NET Standard                                                               |          | ●
.NET Core                                                                   |          | ●
.NET Framework                                                              | ●        | ◖  | No designer/AppModel support for new project system
**App Models**                                                              |
ASP.NET Core (.NET Framework & .NET Core)                                   |          | ●
ASP.NET                                                                     | ●        |   
Xamarin                                                                     | ●        |   
Universal Windows Platform (UWP)                                            | ●        |   
Windows Presentation Framework (WPF)                                        | ●        |   
Windows Forms                                                               | ●        |   
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
Edit project XML while loaded                                               |          | ●
Automatically reload project file with no prompts                           |          | ●
Automatically reload targets files                                          |          | ●
Automatically refresh Solution Explorer to reflect file system              |          | ●
Show items included in imports (.targets/.props)                            |          | ●
**Dependencies**|
Auto-restore packages on load and external edit                             |          | ● 
PackageReference support                                                    | ◖       | ● | Legacy does not reload package targets file without VS restart. Also does not support using MSBuild properties in name, version and metadata.
Dependency node showing package/project graph                               |          | ● 
Transitive ProjectReference                                                 |          | ●
Generate NuGet package on build                                             |          | ● 
**Features**|
Add Service Reference                                                       | ●        | 
Add Web Reference                                                           | ●        | 
Add Data Source                                                             | ●        | 
"Initialize Interactive Windows with Project"                               | ●        | 
Class Diagrams                                                              | ●        | ◖
Code Analysis                                                               | ●        | 
Code Metrics                                                                | ●        | 
Fakes                                                                       | ●        | 
T4 Templates                                                                | ●        | 
Zero Impact Projects ("Save new projects when created")                     | ●        | 
Simplified configurations ("Show advanced build configurations")            | ●        | 
