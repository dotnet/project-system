# C#, F# and Visual Basic project system

|Release|Branch|Unit Tests (Debug)|Unit Tests (Release)|Visual Studio Tests (Debug)|Visual Studio Tests (Release)
|---|---|:--:|:--:|:--:|:--:|
|[15.0.x](https://github.com/dotnet/project-system/milestone/4)|[dev15.0.x](docs/repo/roadmap.md)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.0.x/job/windows_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.0.x/job/windows_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.0.x/job/windows_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.0.x/job/windows_release/)|
|[15.2](https://github.com/dotnet/project-system/milestone/14)|[dev15.2.x](docs/repo/roadmap.md)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.2.x/job/windows_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.2.x/job/windows_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.2.x/job/windows_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.2.x/job/windows_release/)|
|[15.3 Preview](https://github.com/dotnet/project-system/milestone/15)|[dev15.3.x](docs/repo/roadmap.md)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_release/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_integration_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_integration_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_integration_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.3.x/job/windows_integration_release/)|
|[15.3](https://github.com/dotnet/project-system/milestone/7)|[master](docs/repo/roadmap.md)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_release/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_integration_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_integration_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_integration_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/master/job/windows_integration_release/)|
|[15.6](https://github.com/dotnet/project-system/milestone/16)|[dev15.6](docs/repo/roadmap.md)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_release/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_integration_debug/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_integration_debug/)|[![Build Status](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_integration_release/badge/icon)](https://ci.dot.net/job/dotnet_project-system/job/dev15.6/job/windows_integration_release/)|

This repository contains the new C#, F# and Visual Basic project system that has been rewritten on top of the [Common Project System (CPS)](https://github.com/microsoft/vsprojectsystem). In [Visual Studio 2017](https://www.visualstudio.com/vs/), this project system is used by default for Shared Projects (C# and Visual Basic), and .NET Core (C#) project types, however, [long term](docs/repo/roadmap.md) it will be the basis of all C#, F# and Visual Basic project types.

The existing C# and Visual Basic project systems (csproj.dll and msvbprj.dll), which first shipped back in Visual Studio.net nearly 15 years ago, have served us well but are:

- Native and COM-based
- Single threaded and bound to the UI thread
- Hard to extend outside of aggregation via the use of `<ProjectTypeGuids>` and [sub types (flavors)](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/project-types)
- Tied to Visual Studio

The new C#, F# and Visual Basic project system is:

- Managed and managed-interface based
- Multi-threaded, scalable, and responsive
- Easy to extend via the use of the  Managed Extensibility Framework (MEF) and composable. Many parties, including 3rd parties, can contribute to a single project system
- Hostable outside of Visual Studio

## What is a project system?
A project system sits between a project file on disk (for example, .csproj and .vbproj) and various Visual Studio features including, but not limited to, Solution Explorer, designers, the debugger, language services, build and deployment. Almost all interaction that occurs with files contained in a project file, happens through the project system.

There are many technologies that come together to make up the .NET project system:

- [MSBuild](https://github.com/microsoft/msbuild) provides the build engine and file format.
- [SDK](https://github.com/dotnet/sdk) provides the MSBuild tasks and targets needed to build .NET projects.
- [Common Project System](https://github.com/microsoft/vsprojectsystem) provides the base building blocks for the project system including (but not limited to) project tree, build and debugger coordination and Visual Studio integration.
- [Roslyn](https://github.com/dotnet/roslyn) provides C# and Visual Basic language support including compilers, IntelliSense, refactorings, analyzers and code fixes.
- [Visual F# tools](https://github.com/Microsoft/visualfsharp) provides F# language support.
- [CLI](https://github.com/dotnet/cli) is the .NET command-line interface for building, running and interacting with .NET projects.

![image](https://cloud.githubusercontent.com/assets/1103906/24277819/d1e48eba-1093-11e7-811f-ae5debcc1e6c.png)

## How do I engage and contribute?
We welcome you to try things out, [file issues](https://github.com/dotnet/roslyn-project-system/issues), make feature requests and join us in design conversations. If you are looking for something to work on, take a look at our [up-for-grabs issues](https://github.com/dotnet/roslyn-project-system/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+for+Grabs%22) for a great place to start. Also be sure to check out our [contributing guide](CONTRIBUTING.md).

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see [Contributors Code of conduct](https://github.com/dotnet/home/blob/master/guidance/be-nice.md). 
