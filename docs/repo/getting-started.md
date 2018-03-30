# Getting Started

## Code

### Command-line

From within a [Visual Studio Developer Prompt](https://msdn.microsoft.com/en-us/library/ms229859(v=vs.150).aspx), from the repo root, run:

```
build.cmd
```

This builds, runs tests and deploys to Visual Studio.

### Visual Studio
From within [Visual Studio 2017](https://www.visualstudio.com/downloads/), simply open _ProjectSystem.sln_.

Inside Visual Studio, you can build, run tests and deploy.

__NOTE:__ To workaround a bug preventing Visual Studio from restoring this repro, run `build.cmd` once from a Visual Studio Developer Prompt, as called out above.


## Debugging/Deploying

By default when you build inside Visual Studio or the command-line, the project system and other binaries gets deployed to the _ProjectSystem_ experimental instance of Visual Studio. They are setup so that they _override_ any binaries that come with Visual Studio.

### Command-line

From the command-line, after you've run `build.cmd`, you can launch a Visual Studio instance with your recently built bits by running the following from a Visual Studio Command Prompt:

```
REM Set these to make sure we respect the rules/targets from the repo instead of the product (replacing `[RepoRoot]`)
set VisualStudioXamlRulesDir=[RepoRoot]\artifacts\Debug\VSSetup\Rules\
set VisualBasicDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.VisualBasic.DesignTime.targets
set FSharpDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.FSharp.DesignTime.targets
set CSharpDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.CSharp.DesignTime.targets

devenv /rootsuffix ProjectSystem
```

### Visual Studio

To start debugging:

1. Open __ProjectSystem.sln__
2. Right-click on the __ProjectSystemSetup__ project, and choose __Set As Startup Project__
3. Press _F5_

If this is your first launch of the project system, or _ProjectSystem_ experimental instance, press _CTRL+F5_ to pre-prime and avoid a _long_ start up time.

For tips, see [Debugging Tips](debugging-tips.md)

## Testing 

### Project System
While the long term goal is to have all C#, F# and Visual Basic projects use this project system, currently only .NET Core, .NET Standard and Shared Projects do. If you want to test other project types, you can manually create a project to test this:

1. __File__ -> __New__ -> __Project__ -> __C#__ -> __Templates__ -> __Visual C#__ -> __Windows__ -> __Console App (.NET Framework)__
2. Right-click on the project and choose __Open in File Explorer__
3. __File__ -> __Close Solution__
4. In __File Explorer__, rename project from _[project].csproj_ -> _[project].msbuildproj_
5. __File__ -> __Open__ -> __Project/Solution__ and browse to the project you just renamed and choose __Open__

### AppDesigner, Settings, Resource Editors and Property Pages
Both the new project system and the existing project system use the features built from this repository.

## Code Coverage

### Visual Studio

You can collect code coverage within Visual Studio, to do so, do the following:

1. __Test__ -> __Test Settings__ -> __Select Test Settings File__
2. In __Open Settings Files__, browse to and select _src\CodeCoverage.runsettings_. This will exclude files from the coverage run that are not part of the product.
3. Choose __Test__ -> __Analyze Code Coverage__ -> __All Tests__
