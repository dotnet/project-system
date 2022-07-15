# `setup` directory
## General Notes
- Projects containing a `.csproj` & `.vsixmanifest` are VS extension projects, which create a VSIX package.
  - Allows for F5 experience
  - Doesn't require admin access to install
  - Can only install into 8 specific VS sub-directories
- Projects containing a `.swixproj` & `.swr` are loose files to be put into a VSIX package (**not** a VS extension).
  - No F5 experience
  - Requires admin access to install
  - Can install into any sub-directory in VS
- `.vsmanproj` files are for creating the `.vsman` files, necessary for VS insertions.
- `.pkgdef` files are the same as a `.reg` files.
- `.vsmand` files are the definition files that seed the generation of `.vsman` files. They contain the basic structure and information that is added to when creating a fully fleshed-out `.vsman` file.

## Directory Structure
### `Common`
- This directory only contains [ProvideCodeBaseBindingRedirection.cs](Common\ProvideCodeBaseBindingRedirection.cs) which is shared between both the **ProjectSystemSetup** and **VisualStudioEditorsSetup** projects.

### `Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles`
- This directory contains the **Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles** project, which creates the `Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles.vsix` package.
- The `.csproj` is only used to drive the build of the `.swixproj`, which is the actual project that creates the `.vsix` package.
  - Allows the dependent project, [Microsoft.VisualStudio.ProjectSystem.Managed.csproj](..\src\Microsoft.VisualStudio.ProjectSystem.Managed\Microsoft.VisualStudio.ProjectSystem.Managed.csproj), to be built prior to running the `.swixproj` build.
  - The assembly produced from the `.csproj` is not used.
  - The `.swixproj` cannot be added to the [ProjectSystem.sln](..\ProjectSystem.sln) solution.
- The `CommonFiles.swr` is currently *manually updated* when XAML rule files are added.
  - *TODO*: Consider generating this file.
- `ext.xproj.swr` should be removed via: https://github.com/dotnet/project-system/issues/8268
- This project's VSIX package is inserted into VS via the **ProjectSystemSetup** project.

### `ProjectSystemSetup`
- This directory contains the **ProjectSystemSetup** project, which creates the `ProjectSystem.vsix` package.
  - Note that the VSIX package does not contain the word *Setup* within it.
- This project is an SDK-style VS Extension project, creating the VSIX package defined by the `source.extension.vsixmanifest`.
  - This project type is able to build via a workaround in our [Directory.Build.targets](..\Directory.Build.targets) that loads the `Microsoft.VsSDK.targets` manually.
  - The assembly produced from the `.csproj` is not used.
  - The assemblies from [Microsoft.VisualStudio.ProjectSystem.Managed.csproj](..\src\Microsoft.VisualStudio.ProjectSystem.Managed\Microsoft.VisualStudio.ProjectSystem.Managed.csproj) and [Microsoft.VisualStudio.ProjectSystem.Managed.VS.csproj](..\src\Microsoft.VisualStudio.ProjectSystem.Managed.VS\Microsoft.VisualStudio.ProjectSystem.Managed.VS.csproj) are included in the produced VSIX package.
- This project drives our debugging (F5) experience for the repo via the [launchSettings.json](ProjectSystemSetup\Properties\launchSettings.json).
- The `.vsmanproj` creates the `.vsman` manifest, which is inserted into VS as `Microsoft.VisualStudio.ProjectSystem.Managed.vsman`.
  - This manifest also contains the `Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles.vsix` within it.

### `VisualStudioEditorsSetup`
- This directory contains the **VisualStudioEditorsSetup** project, which creates the `VisualStudioEditorsSetup.vsix` package.
- This project is an SDK-style VS Extension project, creating the VSIX package defined by the `source.extension.vsixmanifest`.
  - This project type is able to build via a workaround in our [Directory.Build.targets](..\Directory.Build.targets) that loads the `Microsoft.VsSDK.targets` manually.
  - The assembly produced from the `.csproj` is not used.
  - The assemblies from [Microsoft.VisualStudio.AppDesigner.csproj](..\src\Microsoft.VisualStudio.AppDesigner\Microsoft.VisualStudio.AppDesigner.vbproj) and [Microsoft.VisualStudio.Editors.csproj](..\src\Microsoft.VisualStudio.Editors\Microsoft.VisualStudio.Editors.vbproj) are included in the produced VSIX package.
- The `.vsmanproj` creates the `.vsman` manifest, which is inserted into VS as `Microsoft.VisualStudio.Editors.vsman`.

## High-level Design
The list below is not all-encompassing of the files produced by each project. It is only meant as a high-level overview of what is produced by these projects.

- `ProjectSystemSetup` produces:
  - `Microsoft.VisualStudio.ProjectSystem.Managed.vsman` references:
    - ProjectSystem.vsix
    - Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles.vsix
  - `ProjectSystem.vsix` contains:
    - extension.vsixmanifest
    - Microsoft.VisualStudio.ProjectSystem.Managed.dll
    - Microsoft.VisualStudio.ProjectSystem.Managed.VS.dll
- `VisualStudioEditorsSetup` produces:
  - `Microsoft.VisualStudio.Editors.vsman` references:
    - VisualStudioEditorsSetup.vsix
  - `VisualStudioEditorsSetup.vsix` contains:
    - extension.vsixmanifest
    - Microsoft.VisualStudio.AppDesigner.dll
    - Microsoft.VisualStudio.Editors.dll
- `Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles` produces:
  - `Microsoft.VisualStudio.ProjectSystem.Managed.CommonFiles.vsix` contains:
    - `.xaml` rule files from *Microsoft.VisualStudio.ProjectSystem.Managed*