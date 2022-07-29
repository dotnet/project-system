# `eng` directory
## Directory Structure
### `imports`
- This directory is highly accessed as part of the MSBuild pipeline to produce our assemblies, run tests, create packages, etc.
- *Contents*: This directory contains a majority of the `.props`/`.targets` files in the repo. There are also a couple of `.snk` files used for StrongName signing within the [StrongName.targets](imports\StrongName.targets) file.

#### Notable Files
- [RepoLayout.props](imports\RepoLayout.props): Contains properties for paths to the directories within the repo itself.
- [Packages.targets](imports\Packages.targets): Contains the `RestoreSources` (package feeds) and `PackageReference` nodes utilized by the repo. The `PackageReference` nodes contain the versions of the packages used by the repo, and does not *Include* the packages; they only *Update* them.
  - This file primarily works with [HostAgnostic.props](imports\HostAgnostic.props) which is the file that actually *Includes* many of these packages in the build pipeline.
  - *Potential change*: This file may be replaced with a `Directory.Packages.props` file within: https://github.com/dotnet/project-system/issues/8238

### `pipelines`
- This directory is only used by Azure Pipelines and is not read by anything else in the repo.
- *Contents*: This directory primarily contains `.yaml` files, which become pipelines in Azure Pipelines. There are some other files that support the pipelines within this folder.
  - *Potential change*: Rename this folder to `pipelines` as part of: https://github.com/dotnet/project-system/issues/7915

#### Notable Files
- [official.yml](pipelines\official.yml): This file is our official build pipeline that produces the signed packages (VSIX) that used for insertion into Visual Studio.
- [unit-tests.yml](pipelines\unit-tests.yml): This file is our build pipeline used when validating pull requests into the repo from GitHub.

### `scripts`
- This directory contains:
  - A few files related to running [OptProf](https://aka.ms/OptProf) on the assemblies in our repo.
    - A majority of the configuration relates to setting the test(s) that OptProf uses for creating optimization data.
    - The *runsettings* folder contains the `.runsettings` file. This entire folder is published in our [official.yml](pipelines\official.yml).

### `tools`
- This directory contains the **OneLocBuildSetup** project which creates the `LocProject.json` and copies language-specific XLF files to become language-neutral; both of these are required for the [OneLocBuild](https://aka.ms/OneLocBuild) process for localization.
  - The **OneLocBuildSetup** project is only build and used as part of the [one-loc-build.yml](pipelines\one-loc-build.yml) pipeline.
    - *Potential change*: This pipeline might be combined into another pipeline as part of: https://github.com/dotnet/project-system/issues/7915