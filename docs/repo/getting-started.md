# Getting Started

#### Prerequisites
- [Visual Studio 16.3 Preview 2 or higher](https://visualstudio.microsoft.com/vs/preview/)
- GitHub account
- Basic Git experience: https://docs.github.com/get-started/quickstart/set-up-git

All commands below are run under a [Visual Studio Developer Prompt](https://msdn.microsoft.com/en-us/library/ms229859(v=vs.150).aspx).

## Code

Contribution to this repository is via the [fork model](https://docs.github.com/get-started/quickstart/fork-a-repo). Contributors push changes to their own "forked" version of project-system, and then submit a pull request into it requesting those changes be merged.

To get started:

1. Fork the repo by clicking Fork in the top right corner:

![image](https://user-images.githubusercontent.com/1103906/44329309-7ab55d00-a4a7-11e8-9d1f-74a91f5229f5.png)

2. From a Visual Studio Developer Prompt, run (replacing _[user-name]_ with your GitHub user name):

```
\> git clone https://github.com/[user-name]/project-system
\> cd project-system
\project-system> git remote add upstream https://github.com/dotnet/project-system
\project-system> git remote set-url --push upstream no_push
```

The last command prevents an accidental push to this repository without going through a pull request.

After running above, `git remote -v` should show something similar to the following:
```
\project-system> git remote -v 

origin  https://github.com/davkean/project-system (fetch)
origin  https://github.com/davkean/project-system (push)
upstream        https://github.com/dotnet/project-system (fetch)
upstream        no_push (push)
```

## Build

### Command-line

From within a [Visual Studio Developer Prompt](https://docs.microsoft.com/en-us/dotnet/framework/tools/developer-command-prompt-for-vs), from the repo root, run:

```
project-system> build.cmd
```

This builds, runs tests and deploys to an experimental instance of Visual Studio.

### Visual Studio
From within [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/), open _ProjectSystem.sln_.

Inside Visual Studio, you can build, run tests and deploy.

## Debugging/Deploying

By default when you build inside Visual Studio or the command-line, the project system and other binaries gets deployed to the _Exp_ experimental instance of Visual Studio. They will automatically _override_ any binaries that come with Visual Studio when you launch that instance.

First of all, [setup your debugging environment](/docs/repo/debugging/setting-up-environment.md).

### Command-line

From the command-line, after you've run `build.cmd` you can launch a Visual Studio instance with your recently built bits with:

```
project-system> launch.cmd
```

### Visual Studio

To start debugging:

1. Open __ProjectSystem.sln__
2. Press _F5_

If this is your first launch of the project system, or _ProjectSystem_ experimental instance, press _CTRL+F5_ to pre-prime and avoid a _long_ start up time.

For tips, see [Debugging Tips](debugging-tips.md)

### Deploying to a different hive

When testing inconjunction with other repositories, it's handy to be able to deploy to the same hive so that you can test them together.

Both Visual Studio and command-line respect the `ROOTSUFFIX` environment variable:

```
project-system> set ROOTSUFFIX=RoslynDev

project-system> build.cmd
project-system> launch.cmd
```

```
project-system> set ROOTSUFFIX=RoslynDev

project-system> devenv ProjectSystem.sln
```

Alternatively, both `build.cmd` and `launch.cmd` provide a `/rootsuffix` switch:

``` 
project-system> build.cmd /rootsuffix RoslynDev
project-system> launch.cmd /rootsuffix RoslynDev
```

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
