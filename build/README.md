# `build` directory
## Directory Structure
### `ci`
- This directory is only used by Azure Pipelines and is not read by anything else in the repo.
- *Contents*: This directory primarily contains `.yaml` files, which become pipelines in Azure Pipelines. There are some other files that support the pipelines within this folder.
  - *Potential change*: Rename this folder to `pipelines` as part of: https://github.com/dotnet/project-system/issues/7915

#### Notable Files
- [official.yml](build\official.yml): This file is our official build pipeline that produces the signed packages (VSIX) that used for insertion into Visual Studio.
- [unit-tests.yml](build\unit-tests.yml): This file is our build pipeline used when validating pull requests into the repo from GitHub.

### `import`
- This directory is highly accessed as part of the MSBuild pipeline to produce our assemblies, run tests, create packages, etc.
- *Contents*: This directory contains a majority of the `.props`/`.targets` files in the repo. There are also a couple of `.snk` files used for StrongName signing within the [StrongName.targets](import\StrongName.targets) file.

#### Notable Files
- [Versions.props](import\Versions.props): Contains properties for version information that is used by the build pipeline.
- [RepoLayout.props](import\RepoLayout.props): Contains properties for paths to the directories within the repo itself.
- [Packages.targets](import\Packages.targets): Contains the `RestoreSources` (package feeds) and `PackageReference` nodes utilized by the repo. The `PackageReference` nodes contain the versions of the packages used by the repo, and does not *Include* the packages; they only *Update* them.
  - This file primarily works with [HostAgnostic.props](import\HostAgnostic.props) which is the file that actually *Includes* many of these packages in the build pipeline.
  - *Potential change*: This file may be replaced with a `Directory.Packages.props` file within: https://github.com/dotnet/project-system/issues/8238

### `loc`
- This directory contains the **OneLocBuildSetup** project which creates the `LocProject.json` and copies language-specific XLF files to become language-neutral; both of these are required for the [OneLocBuild](https://aka.ms/OneLocBuild) process for localization.
- The **OneLocBuildSetup** project is only build and used as part of the [one-loc-build.yml](build\one-loc-build.yml) pipeline.
  - *Potential change*: This pipeline might be combined into another pipeline as part of: https://github.com/dotnet/project-system/issues/7915

### `optprof`
- This directory contains a few files related to running [OptProf](https://aka.ms/OptProf) on the assemblies in our repo.
  - A majority of the configuration relates to setting the test(s) that OptProf uses for creating optimization data.
  - *Potential change*: This directory may either be removed *or* have more files added to it as part of: https://github.com/dotnet/project-system/issues/8139

### `proj`
- This directory contains [Build.proj](proj\Build.proj) and [CoreBuild.proj](proj\CoreBuild.proj) which build the projects within the repo.
  - *Potential change*: This directory will likely be removed within: https://github.com/dotnet/project-system/issues/7868

### `script`
- This directory only contains [SetVSEnvironment.cmd](script\SetVSEnvironment.cmd) which is only used by [build.cmd](..\build.cmd).
  - *Potential change*: This directory will likely be removed within: https://github.com/dotnet/project-system/issues/7868