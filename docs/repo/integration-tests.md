#### VSI (Visual Studio Integration) tests

Our VSI tests validate the end-to-end dotnet core project system and sdk scenarios in Visual Studio using Tao-based tests. The test tool and test sources are located in the private roslyn-interal repo [here](https://github.com/dotnet/roslyn-internal/tree/master/Closed/Hosting/RoslynTaoActions/IntegrationTests). Search for the tests ending with "_NetCore.xml" suffix. You can find the currently disabled netcore tests with the corresponding tracking bug over [here](https://github.com/dotnet/roslyn-internal/blob/master/Closed/Hosting/Test/Execution/TestLocator.cs#L54).

#### Local setup for executing VSI tests

1. Enlist into the following dotnet repos and sync to the relevant branch/commits:
  1. [roslyn-project-system](https://github.com/dotnet/roslyn-project-system): Sync to the desired commit to test.
  2. [sdk](https://github.com/dotnet/sdk): Sync to a known LKG commit from the master branch, though this might change to latest commit from the master branch in future. Search for 'https://github.com/dotnet/sdk' in [netci.groovy](/netci.groovy) and the corresponding branch invocation for the commit ID.
  3. [roslyn-internal](https://github.com/dotnet/roslyn-internal): Sync to a known LKG commit from the master branch. Search for 'https://github.com/dotnet/roslyn-internal' in [netci.groovy](/netci.groovy) and the corresponding branch invocation for the commit ID.
2. Build [roslyn-project-system](https://github.com/dotnet/roslyn-project-system) using the build.cmd at the root of the repo. This should also deploy the built VSIXes into the RoslynDev hive. For further guidance, see the 'batchFile' with comment header "Build roslyn-project-system repo" in [netci.groovy](/netci.groovy).
3. Patch all the MSBuild xaml and targets files from the current roslyn-project-system build into VS install. For further guidance, see the 'batchFile' with comment header "Patch all the MSBuild xaml and targets" in [netci.groovy](/netci.groovy).
4. Build [sdk](https://github.com/dotnet/sdk) using the build.cmd at the root of the repo. For further guidance, see the 'batchFile' with comment header "Build sdk repo" in [netci.groovy](/netci.groovy).
5. Install the sdk templates built in the above step into RoslynDev hive. For further guidance, see the 'batchFile' with comment header "install templates into RoslynDev hive" in [netci.groovy](/netci.groovy).
6. Build [roslyn-internal](https://github.com/dotnet/roslyn-internal) and run the dotnet core tao tests using the BuildAndTest.cmd at the root of the repo. This will execute all the dotnet core VSI tests. Alternatively, you can build the repo using build.cmd at the root of the repo and then run individual tests by directly invoking Tao.exe, which is generated at `\roslyn-internal\Open\Binaries\$(Configuration)\Exes\EditorTestApp\Tao.exe` by build. Execute "Tao.exe /?" for help. For further guidance on BuildAndTest.cmd, see the 'batchFile' with comment header "Build roslyn-internal and run netcore VSI tao tests" in [netci.groovy](/netci.groovy).
7. Revert the patched MSBuild xaml and targets files from the VS install. For further guidance, see the 'batchFile' with comment header "Revert patched targets and rules from backup" in [netci.groovy](/netci.groovy).

Contact mavasani in case you hit any issues.
