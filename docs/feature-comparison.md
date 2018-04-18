# Feature Comparison

The following is an incomplete list of features differences between the legacy project system and the new project system. 

**Feature**|**Legacy**|**New**|**Notes**
---|---|---|---
**Platforms**                                                               |
.NET Standard                                                               | No       | Yes
.NET Core                                                                   | No       | Yes
.NET Framework                                                              | Yes      | Partial  | No designer/AppModel support for new project system
**App Models**                                                              |
ASP.NET Core (.NET Framework & .NET Core)                                   | No       | Yes
ASP.NET                                                                     | Yes      | No
Xamarin                                                                     | Yes      | No
Universal Windows Platform (UWP)                                            | Yes      | No
Windows Presentation Framework (WPF)                                        | Yes      | No
Windows Forms                                                               | Yes      | No
**Build**|
Target multiple frameworks (multi-target) from single project               | No       | Yes
Show build (design-time) errors & warnings in Error List as you make them   | No       | Yes
**Debug**|
Debug multiple frameworks from single project                               | No       | Yes
Debug with multiple environments from single project ("launch profiles")    | No       | Yes
Debug settings persistence                                                  |project.csproj.user (per user, per machine)|launchsettings.json (source control)
Modify environment variables on debug                                       | No       | Yes
Launch with native debugging                                                | Yes      | Partial | Need to put `"nativeDebugging": true` in launchsettings.json for new project system
Launch with SQL Server debugging                                            | Yes      | No
Launch with remote debugging                                                | Yes      | No
Launch with Azure Snapshot Debugger                                         | No       | Yes
**Publish**                                                                 |
Publish to Azure                                                            | No       | Yes
ClickOnce Publish                                                           | Yes      | No
**Project**                                                                 |
Globbing support                                                            | No       | Yes    | `<Compile Include="*.cs" />`
Simplified project format                                                   | No       | Yes    | `<Project Sdk="Microsoft.Net.Sdk">`
Simplified configuration syntax                                             | No       | Yes    | `<Configurations>Debug;Release<Configurations>;<Platforms>AnyCPU;x64</Platforms>`
Edit project XML while loaded                                               | No       | Yes
Automatically reload project file with no prompts                           | No       | Yes
Automatically reload targets files                                          | No       | Yes
Automatically refresh Solution Explorer to reflect file system              | No       | Yes
Show items included in imports (.targets/.props)                            | No       | Yes
**Dependencies**|
Auto-restore packages on load and external edit                             | No       | Yes 
PackageReference support                                                    | Partial  | Yes | Legacy does not reload package targets file without VS restart. Also does not support using MSBuild properties in name, version and metadata.
Dependency node showing package/project graph                               | No       | Yes 
Transitive ProjectReference                                                 | No       | Yes
Generate NuGet package on build                                             | No       | Yes 
**Features**|
Add Service Reference                                                       | Yes      | No
Add Web Reference                                                           | Yes      | No
Add Data Source                                                             | Yes      | No
"Initialize Interactive Windows with Project"                               | Yes      | No
Class Diagrams                                                              | Yes      | Partial
Code Analysis                                                               | Yes      | No
Code Metrics                                                                | Yes      | No
Fakes                                                                       | Yes      | No
T4 Templates                                                                | Yes      | No
Zero Impact Projects ("Save new projects when created")                     | Yes      | No
Simplified configurations ("Show advanced build configurations")            | Yes      | No
