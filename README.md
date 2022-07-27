# The .NET Project System for Visual Studio

| Release             | Unit Tests (Debug)                      | Unit Tests (Release)                      | Localization
|---------------------|:---------------------------------------:|:-----------------------------------------:|:------------:
| [16.11][1611Branch] | [![Build Status][1611Debug]][1611Build] | [![Build Status][1611Release]][1611Build] | [![Build Status][1611Spanish]][1611Build]
| [17.0][170Branch]   | [![Build Status][170Debug]][170Build]   | [![Build Status][170Release]][170Build]   | [![Build Status][170Spanish]][170Build]
| [17.1][171Branch]   | [![Build Status][171Debug]][171Build]   | [![Build Status][171Release]][171Build]   | [![Build Status][171Spanish]][171Build]
| [17.2][172Branch]   | [![Build Status][172Debug]][172Build]   | [![Build Status][172Release]][172Build]   | [![Build Status][172Spanish]][172Build]
| [17.3][173Branch]   | [![Build Status][173Debug]][173Build]   | [![Build Status][173Release]][173Build]   | [![Build Status][173Spanish]][173Build]
| [main][MainBranch]  | [![Build Status][MainDebug]][MainBuild] | [![Build Status][MainRelease]][MainBuild] | [![Build Status][MainSpanish]][MainBuild]

[![Join the chat at https://gitter.im/dotnet/project-system](https://badges.gitter.im/dotnet/project-system.svg)](https://gitter.im/dotnet/project-system?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This repository contains the .NET Project System for [Visual Studio](https://www.visualstudio.com/vs/) that is written on top of the [Common Project System (CPS)](https://github.com/microsoft/vsprojectsystem) framework. In Visual Studio 2017, Visual Studio 2019, and Visual Studio 2022, this project system is used for .NET Core [SDK-style] (C#, F# and Visual Basic) and Shared Projects (C# and Visual Basic) project types. In the [long term](docs/repo/roadmap.md), this project system will be the basis for all C#, F# and Visual Basic project types. For a list of feature differences between the project systems, see [Feature Comparison](docs/feature-comparison.md).

The legacy C# and Visual Basic project systems (*csproj.dll* and *msvbprj.dll*) first shipped with Visual Studio .NET in 2002. They have served us well but are:

- Native and COM-based
- Single threaded and bound to the UI thread
- Hard to extend outside of aggregation via the use of `<ProjectTypeGuids>` and [sub types (flavors)](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/project-types)
- Separate implementations for C# and Visual Basic projects

The current .NET Project System is:

- Managed and managed-interface based
- Multi-threaded, scalable, and responsive
- Easy to extend and compose via the Managed Extensibility Framework (MEF). Many parties, including 3rd parties, can contribute to a single project system.
- A single implementation for C#, F# and Visual Basic projects

## What is a project system?
A project system sits between a project file on disk (for example, *.csproj* and *.vbproj*) and various Visual Studio features including, but not limited to, Solution Explorer, designers, the debugger, language services, build and deployment. Almost all interaction that occurs with files contained in a project file happens through the project system.

There are many technologies that come together to make up the .NET Project System:

- [MSBuild](https://github.com/dotnet/msbuild) provides the build engine and file format.
- [SDK](https://github.com/dotnet/sdk) provides the command-line interface for building, running and interacting with .NET projects, along with the necessary MSBuild tasks and targets.
- [Common Project System](https://github.com/microsoft/vsprojectsystem) provides the base building blocks for the project system including (but not limited to) project tree, build and debugger coordination and Visual Studio integration.
- [Roslyn](https://github.com/dotnet/roslyn) provides C# and Visual Basic language support including compilers, IntelliSense, refactorings, analyzers, and code fixes.
- [Visual F# tools](https://github.com/dotnet/fsharp) provides F# language support.

![image](docs/repo/images/solution-explorer.png)

## How do I build the repository?
This repository is built on .NET Framework and requires the .NET Framework version of [MSBuild](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild?view=vs-2022) to build successfully. Additionally, there is a dependency on the [Visual Studio SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/starting-to-develop-visual-studio-extensions?view=vs-2022) as the .NET Project System is bundled as a Visual Studio Extension for deployment into Visual Studio.

Here are the two ways to acquire the necessary components:
- Install the latest [Visual Studio](https://visualstudio.microsoft.com/downloads/)
- Install [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/?q=build+tools#build-tools-for-visual-studio-2022)

For either option, these workloads should be installed:
- .NET desktop build tools
- Visual Studio extension development

![image](docs/repo/images/workloads-for-building-the-repo.png)

After the necessary components are installed, simply run the `build.cmd` batch file at the root of the repository. This will build, test, and bundle the repository appropriately.

**NOTE:** When using the *Build Tools for Visual Studio*, the basic call to **build.cmd** will fail. It will attempt to deploy to the *Exp* hive (for debugging the .NET Project System) which it cannot do if full Visual Studio is not installed. Additionally, some tests will fail. To build without failures in this situation, run `build.cmd /p:DeployExtension=false /p:Test=false`.

### **build.cmd** flags
All the command line arguments provided to **build.cmd** get forwarded to MSBuild. There are some special properties we've set up for building this repo.
- For Projects:
  - `/p:SrcProjects=[true or false]`: Includes the projects within the **src** directory. Default: `true`
  - `/p:TestProjects=[true or false]`: Includes the projects within the **tests** directory. Default: `true`
  - `/p:SetupProjects=[true or false]`: Includes the projects within the **setup** directory. Default: `true`
- For Targets:
  - `/p:Restore=[true or false]`: Runs the **Restore** target to acquire project dependencies. Default: `true`
  - `/p:Build=[true or false]`: Runs the **Build** target to compile the projects into assemblies. Default: `true`
  - `/p:Rebuild=[true or false]`: Runs the **Rebuild** target which cleans and builds the projects. Default: `false`
  - `/p:Test=[true or false]`: Runs the **Test** target to execute the xUnit test projects. Default: `true`
  - `/p:Pack=[true or false]`: Runs the **Pack** target to bundle the projects into NuGet packages. Default: `true`

## How do I engage and contribute?
We welcome you to try things out, [file issues](https://github.com/dotnet/project-system/issues), make feature requests, and join us in design conversations. If you are looking for something to work on, take a look at our [help wanted issues](https://github.com/dotnet/project-system/issues?q=is%3Aopen+is%3Aissue+label%3A%22Help+Wanted%22) for a great place to start. Also, check out our [contributing guide](CONTRIBUTING.md).

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information, see [Contributors Code of conduct](https://github.com/dotnet/home/blob/master/guidance/be-nice.md).

<!-- References -->

[MainBranch]: https://github.com/dotnet/project-system/tree/main
[MainDebug]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=main&jobName=Windows_Debug&%20Debug&label=main
[MainRelease]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=main&jobName=Windows_Release&%20Release&label=main
[MainSpanish]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=main&jobName=Spanish&label=main
[MainBuild]: https://dev.azure.com/dnceng/public/_build/latest?definitionId=406&branchName=main

[1611Branch]: https://github.com/dotnet/project-system/tree/dev16.11.x
[1611Debug]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev16.11.x&jobName=Windows_Debug&%20Debug&label=dev16.11.x
[1611Release]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev16.11.x&jobName=Windows_Release&%20Release&label=dev16.11.x
[1611Spanish]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev16.11.x&jobName=Spanish&label=dev16.11.x
[1611Build]: https://dev.azure.com/dnceng/public/_build/latest?definitionId=406&branchName=dev16.11.x

[170Branch]: https://github.com/dotnet/project-system/tree/dev17.0.x
[170Debug]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.0.x&jobName=Windows_Debug&%20Debug&label=dev17.0.x
[170Release]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.0.x&jobName=Windows_Release&%20Release&label=dev17.0.x
[170Spanish]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.0.x&jobName=Spanish&label=dev17.0.x
[170Build]: https://dev.azure.com/dnceng/public/_build/latest?definitionId=406&branchName=dev17.0.x

[171Branch]: https://github.com/dotnet/project-system/tree/dev17.1.x
[171Debug]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.1.x&jobName=Windows_Debug&%20Debug&label=dev17.1.x
[171Release]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.1.x&jobName=Windows_Release&%20Release&label=dev17.1.x
[171Spanish]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.1.x&jobName=Spanish&label=dev17.1.x
[171Build]: https://dev.azure.com/dnceng/public/_build/latest?definitionId=406&branchName=dev17.1.x

[172Branch]: https://github.com/dotnet/project-system/tree/dev17.2.x
[172Debug]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.2.x&jobName=Windows_Debug&%20Debug&label=dev17.2.x
[172Release]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.2.x&jobName=Windows_Release&%20Release&label=dev17.2.x
[172Spanish]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.2.x&jobName=Spanish&label=dev17.2.x
[172Build]: https://dev.azure.com/dnceng/public/_build/latest?definitionId=406&branchName=dev17.2.x

[173Branch]: https://github.com/dotnet/project-system/tree/dev17.3.x
[173Debug]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.3.x&jobName=Windows_Debug&%20Debug&label=dev17.3.x
[173Release]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.3.x&jobName=Windows_Release&%20Release&label=dev17.3.x
[173Spanish]: https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/project-system/unit-tests?branchName=dev17.3.x&jobName=Spanish&label=dev17.3.x
[173Build]: https://dev.azure.com/dnceng/public/_build/latest?definitionId=406&branchName=dev17.3.x