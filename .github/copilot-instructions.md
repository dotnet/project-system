# .NET Project System for Visual Studio

The .NET Project System for Visual Studio is a Windows-based Visual Studio extension project built with C#, VB.NET, MSBuild, and the Common Project System (CPS) framework. This supports SDK-style .NET (C#, F#, and Visual Basic) and Shared Projects project types.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites - CRITICAL
- **WINDOWS ONLY**: This repository requires Windows and Visual Studio. Build WILL FAIL on Linux/macOS.
- Install [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) with these workloads:
  - .NET desktop build tools  
  - Visual Studio extension development
- MINIMUM: Visual Studio 16.3 Preview 2 or higher
- .NET 9.0 SDK (automatically installed with Visual Studio or via [dotnet.microsoft.com](https://dotnet.microsoft.com/download))

### Build and Test
- **PRIMARY BUILD COMMAND**: From a Visual Studio Developer Prompt:
  ```
  build.cmd
  ```
  - NEVER CANCEL: Build takes 15-45 minutes. Set timeout to 60+ minutes.
  - This builds, runs tests, and deploys to experimental VS instance
  - Creates artifacts in `artifacts/` directory with build logs

- **BUILD OPTIONS**:
  ```
  build.cmd /p:Test=false          # Skip tests (faster build)
  build.cmd /p:SetupProjects=false # Skip VS setup projects  
  build.cmd /p:SrcProjects=true /p:TestProjects=false # Source only
  ```

- **RUN TESTS**: Tests are included in default build. To run separately:
  - Tests execute automatically during `build.cmd`
  - NEVER CANCEL: Test suite takes 10-30 minutes. Set timeout to 45+ minutes.
  - Uses xUnit framework
  - Results appear in artifacts directory

### Visual Studio Development
- **OPEN SOLUTION**: Open `ProjectSystem.sln` in Visual Studio 2022
- **F5 DEBUGGING**: Press F5 to launch experimental VS instance with your changes
- **FIRST LAUNCH**: Use Ctrl+F5 for first launch to pre-prime and avoid long startup time
- **BUILD IN VS**: Use Build menu or Ctrl+Shift+B

### Launch and Test Your Changes
- **COMMAND LINE**: After `build.cmd` completes:
  ```
  Launch.cmd
  ```
  - Launches Visual Studio experimental instance with your built bits
  - Default hive is 'Exp' - change with `/rootsuffix` parameter
  
- **DIFFERENT HIVE**: 
  ```
  set ROOTSUFFIX=RoslynDev
  build.cmd
  Launch.cmd /rootsuffix RoslynDev
  ```

## Validation
- **ALWAYS** build and test with `build.cmd` before committing changes
- **MANUAL TESTING**: Create test projects in the experimental VS instance:
  1. File → New → Project → C#/VB.NET → Console App (.NET)
  2. Add/remove references and packages
  3. Build and run the test project
  4. Verify project properties work correctly
- The repository includes both unit tests and integration tests
- Integration tests require Visual Studio experimental instance

## Repository Structure

### Key Directories
- `src/Microsoft.VisualStudio.ProjectSystem.Managed/` - Core project system (C#)
- `src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/` - VS integration layer (C#) 
- `src/Microsoft.VisualStudio.Editors/` - Property pages and designers (VB.NET)
- `src/Microsoft.VisualStudio.AppDesigner/` - Application designer (VB.NET)
- `tests/` - Unit tests and integration tests
- `setup/` - Visual Studio deployment packages (VSIX)
- `eng/` - Build pipelines and tooling
- `docs/` - Documentation including architecture guides

### Key Files
- `ProjectSystem.sln` - Main solution file
- `build.cmd` - Primary build script (Windows batch file)
- `Launch.cmd` - Launches experimental VS instance
- `Directory.Build.props` - MSBuild properties (NetCoreTargetFramework=net9.0)

## Common Issues and Solutions
- **Build fails with package restore errors**: Requires access to internal Azure DevOps feeds - build from Visual Studio Developer Prompt
- **"Visual Studio must be installed"**: Ensure Visual Studio with correct workloads is installed
- **Long build times**: Normal - initial builds take 15-45 minutes, incremental builds are faster
- **Experimental VS won't start**: Run `Launch.cmd` from same prompt where you ran `build.cmd`

## Architecture Overview
- Built on [Common Project System (CPS)](https://github.com/microsoft/VSProjectSystem) framework
- Multi-threaded, MEF-based extensible architecture
- Supports .NET SDK-style projects (C#, F#, VB.NET)
- Integrates with MSBuild, Roslyn, and Visual Studio APIs
- Replaces legacy csproj.dll/msvbproj.dll project systems

## DO NOT attempt to:
- Build on Linux or macOS (Windows/VS only)
- Use `dotnet build` directly (requires Visual Studio MSBuild)
- Skip the Visual Studio Developer Prompt requirement
- Cancel long-running builds/tests (they may take 45+ minutes)

Always validate changes by launching the experimental VS instance and creating/testing real projects.