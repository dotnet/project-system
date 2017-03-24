### Roadmap

The first release of the project system ("15.0") was heavily focused on support .NET Core scenarios and parity with [VS 2015 project.json tooling](https://github.com/dotnet/roslyn-project-system/issues?utf8=%E2%9C%93&q=label%3AParity-XProj%20). This will continue through the Visual Studio 15.x.x updates and releases. In 16.0, we'll start focusing on [feature parity](https://github.com/dotnet/roslyn-project-system/labels/Parity-VSLangProj) with the legacy project systems in csproj.dll and msvbprj.dll. This will ensure a seamless upgrade when customers open existing projects in the new project system and things should feel extremely familiar for existing projects with zero conversions or project upgrades.

|Release|Branches|Description|
|-------|--------|--------|
|[15.0.x](https://github.com/dotnet/roslyn-project-system/milestone/4)|[15.0.1](https://github.com/dotnet/roslyn-project-system/tree/dev15.0.x)|Impactful bugs, crashes and hangs that block major scenarios.
|[15.1](https://github.com/dotnet/roslyn-project-system/milestone/13)|[15.1.x](https://github.com/dotnet/roslyn-project-system/tree/dev15.1.x)|Impactful bugs, crashes and hangs that block minor scenarios.
|[15.2](https://github.com/dotnet/roslyn-project-system/milestone/14)|[15.2.x](https://github.com/dotnet/roslyn-project-system/tree/dev15.2.x)|Impactful bugs, crashes and hangs that block minor scenarios.
|[15.3](https://github.com/dotnet/roslyn-project-system/milestone/7)|[master](https://github.com/dotnet/roslyn-project-system/tree/master)|Support for .NET Core 2.0, .NET Standard 2.0, and other bugs fixes related to .NET Core tooling that do not make above releases.
|[16.0](https://github.com/dotnet/roslyn-project-system/milestone/12)|none|Feature parity with the legacy project system, support for WinForms, WPF and ASP.NET.
|[Unknown](https://github.com/dotnet/roslyn-project-system/milestone/5)|none|Uncommitted features.
