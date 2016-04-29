### Code 

To build, deploy and test, clone this repro and open _src\ProjectSystem.sln_.

Alternatively, if you like the command-line, you can run build.cmd from a [Visual Studio Developer Prompt](https://msdn.microsoft.com/en-us/library/ms229859(v=vs.110).aspx) which builds, deploys and runs tests.

### Debugging/Deploying
By default, the project system gets deployed to the _RoslynDev_ experimental instance of Visual Studio, to debug:

1. Open __src\ProjectSystem.sln__
2. Right-click on the __ProjectSystemSetup__ project, and choose __Set As Startup Project__
3. Press _F5_

If this is your first launch of the project system, or RoslynDev experimental instance, press _CTRL+F5_ to pre-prime and avoid a long start up time.

### Testing

Currently, no C#/VB projects created by templates are CPS-based so you need to manually create a project, the easiest way:

1. __File__ -> __New__ -> __Project__ -> __C#__ -> __Templates__ -> __Visual C#__ -> __Windows__ -> __Console Application__
2. Right-click on the project and choose __Open in File Explorer__
3. __File__ -> __Close Solution__
4. In __File Explorer__, rename project from _[project].csproj_ -> _[project].msbuildproj_
5. __File__ -> __Open__ -> __Project/Solution__ and browse to the project you just renamed and choose __Open__

### Code Coverage

You can collect code coverage within Visual Studio, to do so, do the following:

1. __Test__ -> __Test Settings__ -> __Select Test Settings File__
2. In __Open Settings Files__, browse to and select _src\ProjectSystem.runsettings_. This will exclude files from the coverage run that are not part of the product.
3. Choose __Test__ -> __Analyze Code Coverage__ -> __All Tests__



