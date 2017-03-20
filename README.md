## Welcome to the new C# and Visual Basic project system

|Branch|Debug|Release|
|---|:--:|:--:|
|dev15.0.x|[![Build Status](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.0.x/job/windows_debug/badge/icon)](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.0.x/job/windows_debug/)|[![Build Status](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.0.x/job/windows_release/badge/icon)](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.0.x/job/windows_release/)|
|dev15.1.x|[![Build Status](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.1.x/job/windows_debug/badge/icon)](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.1.x/job/windows_debug/)|[![Build Status](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.1.x/job/windows_release/badge/icon)](http://ci.dot.net/job/dotnet_roslyn-project-system/job/dev15.1.x/job/windows_release/)|
|master|[![Build Status](http://ci.dot.net/job/dotnet_roslyn-project-system/job/master/job/windows_debug/badge/icon)](http://ci.dot.net/job/dotnet_roslyn-project-system/job/master/job/windows_debug/)|[![Build Status](http://ci.dot.net/job/dotnet_roslyn-project-system/job/master/job/windows_release/badge/icon)](http://ci.dot.net/job/dotnet_roslyn-project-system/job/master/job/windows_release/)|

The repository contains the new C# and Visual Basic project system that has been rewritten on top of the [Visual Studio Common Project System (CPS)](https://blogs.msdn.microsoft.com/visualstudio/2015/06/02/introducing-the-project-system-extensibility-preview/). In Visual Studio 2017, this project system is used by default for the Shared Projects, .NET Core and .NET Standard project types, but long term it will be the basis of all C#/VB project types.

The current C# and Visual Basic project systems (csproj.dll and msvbprj.dll), which first shipped back in Visual Studio.net nearly 15 years ago have served us well, but are:

- Native and COM-based
- Single threaded and bound to the UI thread
- Hard to extend outside of aggregation via the use of `<ProjectTypeGuids>` and [sub types (flavors)](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/project-types)
- Tied to Visual Studio

The new C# and Visual Basic project system, built on top of CPS, will be:

- Managed and managed-interface based
- Multi-threaded, scalable, and responsive
- Easy to extend via the use of the  Managed Extensibility Framework (MEF) and composable. Many parties, including 3rd parties, can contribute to a single project system
- Hostable outside of Visual Studio

## What is a project system?
A project system sits between a project file on disk (for example, .csproj and .vbproj) and various Visual Studio features including, but not limited to, Solution Explorer, designers, the debugger, language services, build and deployment. Almost all interaction that occurs with files contained in a project file, happens through the project system.

The C# and Visual Basic project system adds C# and Visual Basic language support to CPS via [Roslyn](http://github.com/dotnet/roslyn).

![image](https://cloud.githubusercontent.com/assets/1103906/14901076/73454a6a-0d48-11e6-8478-472474d55824.png)

## How do I engage and contribute?
We welcome you to try things out, [file issues](https://github.com/dotnet/roslyn-project-system/issues), make feature requests and join us in design conversations. If you are looking for something to work on, take a look at our [up-for-grabs issues](https://github.com/dotnet/roslyn-project-system/issues?q=is%3Aopen+is%3Aissue+label%3A%22Up+for+Grabs%22) for a great place to start. Also be sure to check out our [contributing guide](CONTRIBUTING.md).

This project has adopted a code of conduct adapted from the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](http://contributor-covenant.org/adopters/). For more information see [Contributors Code of conduct](https://github.com/dotnet/home/blob/master/guidance/be-nice.md).
