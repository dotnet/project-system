# The .NET Project System for Visual Studio

| Release             | Build                   | Compliance                   | Publish                   | Localization
|---------------------|:-----------------------:|:----------------------------:|:-------------------------:|:-------------------------:
| [main][MainBranch]  | [![MainBuild]][MainRun] | [![MainCompliance]][MainRun] | [![MainPublish]][MainRun] | [![MainLocalization]][MainRun]

This repository contains the .NET Project System for [Visual Studio](https://visualstudio.microsoft.com/vs/) that is written on top of the [Common Project System (CPS)](https://github.com/microsoft/VSProjectSystem) framework. This project system is used for .NET [SDK-style] (C#, F# and Visual Basic) and Shared Projects (C# and Visual Basic) project types. For a list of feature differences between the project systems, see [Feature Comparison](docs/feature-comparison.md).

The legacy C# and Visual Basic project systems (*csproj.dll* and *msvbprj.dll*) first shipped with Visual Studio .NET in 2002. They have served us well but are:

- Native and COM-based
- Single threaded and bound to the UI thread
- Hard to extend outside of aggregation via the use of `<ProjectTypeGuids>` and [sub types (flavors)](https://learn.microsoft.com/visualstudio/extensibility/internals/project-types)
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
- [Common Project System](https://github.com/microsoft/VSProjectSystem) provides the base building blocks for the project system including (but not limited to) project tree, build and debugger coordination and Visual Studio integration.
- [Roslyn](https://github.com/dotnet/roslyn) provides C# and Visual Basic language support including compilers, IntelliSense, refactorings, analyzers, and code fixes.
- [Visual F# tools](https://github.com/dotnet/fsharp) provides F# language support.

![image](docs/repo/images/solution-explorer.png)

## How do I build the repository?
This repository is built on .NET Framework and requires the .NET Framework version of [MSBuild](https://learn.microsoft.com/visualstudio/msbuild/msbuild?view=vs-2022) to build successfully. Additionally, there is a dependency on the [Visual Studio SDK](https://learn.microsoft.com/visualstudio/extensibility/starting-to-develop-visual-studio-extensions?view=vs-2022) as the .NET Project System is bundled as a Visual Studio Extension for deployment into Visual Studio.

Here is how to acquire the necessary components:
- Install the latest [Visual Studio](https://visualstudio.microsoft.com/downloads/)
  - Select these workloads during installation:
    - .NET desktop build tools
    - Visual Studio extension development

![image](docs/repo/images/workloads-for-building-the-repo.png)

After the necessary components are installed, simply run the `build.cmd` batch file at the root of the repository. This will build, test, and bundle the repository appropriately.

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

This project has adopted a code of conduct adapted from the [Contributor Covenant](https://www.contributor-covenant.org) to clarify expected behavior in our community. This code of conduct has been [adopted by many other projects](https://www.contributor-covenant.org/adopters/). For more information, see [Contributors Code of conduct](https://github.com/dotnet/home/blob/master/guidance/be-nice.md).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

## Data Collection

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in ["Visual Studio Customer Experience Improvement Program"](https://learn.microsoft.com/visualstudio/ide/visual-studio-experience-improvement-program). There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

<!-- References -->

[MainBranch]:       https://github.com/dotnet/project-system/tree/main
[MainBuild]:        https://dev.azure.com/devdiv/DevDiv/_apis/build/status/DotNet/project-system/DotNet-Project-System?branchName=main&label=main&stageName=Build
[MainCompliance]:   https://dev.azure.com/devdiv/DevDiv/_apis/build/status/DotNet/project-system/DotNet-Project-System?branchName=main&label=main&stageName=Compliance
[MainPublish]:      https://dev.azure.com/devdiv/DevDiv/_apis/build/status/DotNet/project-system/DotNet-Project-System?branchName=main&label=main&stageName=Publish
[MainLocalization]: https://dev.azure.com/devdiv/DevDiv/_apis/build/status/DotNet/project-system/DotNet-Project-System?branchName=main&label=main&stageName=Localization
[MainRun]:          https://dev.azure.com/devdiv/DevDiv/_build/latest?definitionId=9675&branchName=main
