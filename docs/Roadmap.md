The first release[1] of the project system will be heavily focused focused on [feature parity](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+label%3A%22Area-Project+System%22+label%3A%22Project+System-Parity%22) with the old project systems in csproj.dll and msvbprj.dll. This ensures there's a seamless upgrade when customers open existing projects in the new project system, things should feel extremely familiar with zero conversions or project "upgrades".

Heading towards the end of the first release, we'll start looking at picking off some [new features](https://github.com/dotnet/roslyn/labels/Project%20System-New%20Feature).

[1] We're following the Roslyn versioning of "2.0".

### Roadmap
|Release|Features|At end of this milestone|
|-------|--------|--------|
|2.0 Preview 3|[Setup, Localization, AppDesigner, Debugging, New Language Service Host, Add Service/Web Reference, Analyzer Dependencies](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Area-Project+System%22+milestone%3A%222.0+%28Preview+3%29%22)|Off by default for all projects, with ability to opt-in|
|2.0 Preview 4|[WPF Flavor replacement, ClickOnce, Simplified Add Item, Up-to-date checks](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Area-Project+System%22+milestone%3A%222.0+%28Preview+4%29%22)|On for all console, library and Windows Forms projects|
|2.0 RC|[NuGet Dependencies, Retargeting](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+is%3Aissue+label%3A%22Area-Project+System%22+milestone%3A%222.0+%28RC%29%22)|On for all console, library, Windows Forms and WPF projects|
