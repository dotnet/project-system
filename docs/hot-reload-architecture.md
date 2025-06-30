# Hot Reload Architecture Documentation
## For AI
Remember the following rules when generating content:
- if implementation and documentation doesn't match, always prefer the implementation and modify the documentation to match the implementation

## For Humans
- Take this document as a reference, but not as a solid ground of truth.
- Include this document in the context if you want to vibe coding hot-reload code. 
- Every time you update the implementation, make sure to update this document as well. You can do this by prompting the AI to update the document with the latest changes in the implementation.

## Overview

Hot Reload is a feature in the Visual Studio / Visual Studio Code that allows developers to apply code changes to a running application without stopping and restarting the debug session. This document describes the architecture and integration points of the Hot Reload system within the .NET project system.

The Hot Reload system enables a faster development experience by:
- Applying code changes in real-time to running processes
- Maintaining application state during updates
- Providing feedback on compilation errors and update success/failure
- Supporting both debugging and non-debugging scenarios

## Major MEF Components

### IProjectHotReloadAgent

**Contract**: [`IProjectHotReloadAgent.cs`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/Contracts/IProjectHotReloadAgent.cs)  
**Implementation**: [`ProjectHotReloadAgent`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/ProjectHotReloadAgent.cs) *(Shared between VS and C# Dev Kit)*  
**Exports**: `IProjectHotReloadAgent`, `IProjectHotReloadAgent2`  
**Scope**: Project Service Level  

The `IProjectHotReloadAgent` is the factory interface responsible for creating Hot Reload sessions. It serves as the entry point for external components that need to create Hot Reload sessions. The enhanced `IProjectHotReloadAgent2` interface provides additional overloads with full context support.

**Key Responsibilities**:
- Factory for creating `ProjectHotReloadSession` instances
- Provides multiple overloads for session creation:
  - Basic session creation with minimal parameters (`IProjectHotReloadAgent`)
  - Enhanced session creation with full launch context - project, launch provider, build manager, launch profile, and debug options (`IProjectHotReloadAgent2`)
- Manages dependencies for session creation (HotReloadAgentManagerClient, DeltaApplierCreator)

**Dependencies**:
- `IHotReloadAgentManagerClient` - Communicates with VS debugger Hot Reload infrastructure
- `IHotReloadDiagnosticOutputService` - Handles output and logging
- `IManagedDeltaApplierCreator` - Creates delta appliers for code changes

### IProjectHotReloadSessionManager

**Contract**: [`IProjectHotReloadSessionManager.cs`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/IProjectHotReloadSessionManager.cs)  
**Implementation**: [`ProjectHotReloadSessionManager`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/ProjectHotReloadSessionManager.cs) *(Shared between VS and C# Dev Kit)*  
**Exports**: `IProjectHotReloadSessionManager`, `IProjectHotReloadUpdateApplier`  
**Scope**: Configured Project Level  
**Tests**: [`ProjectHotReloadSessionManagerTests.cs`](../tests/Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests/ProjectSystem/HotReload/ProjectHotReloadSessionManagerTests.cs)  

The `IProjectHotReloadSessionManager` is the central coordinator interface for Hot Reload functionality within a project. It manages the lifecycle of Hot Reload sessions and ensures proper integration with the project system.

**Key Responsibilities**:
- Session lifecycle management (pending → active → terminated)
- Project capability validation (SupportsHotReload, debug symbols, optimization settings)
- Process monitoring and cleanup
- Environment variable configuration
- Integration with project build and launch systems
- Thread-safe session state management

**Key Methods**:
- `TryCreatePendingSessionAsync()` - Creates a pending session if project supports Hot Reload, taking configured project, launch provider, environment variables, launch options, and launch profile
- `ActivateSessionAsync()` - Activates a pending session when process starts, associating it with process ID, debugger state, and project name
- `ApplyHotReloadUpdateAsync()` - Coordinates updates across all active sessions (if implemented)

### IProjectHotReloadBuildManager

**Contract**: [`IProjectHotReloadBuildManager.cs`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/IProjectHotReloadBuildManager.cs)  
**VS Implementation**: [`ProjectHotReloadBuildManager`](../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/HotReload/ProjectHotReloadBuildManager.cs) *(VS-specific implementation only)*  
**Scope**: Unconfigured Project Level  

Interface responsible for building the project during Hot Reload operations, particularly for restart scenarios.

**Key Responsibilities**:
- Coordinate project builds during Hot Reload restart
- Ensure build dependencies are met before applying changes

### IProjectHotReloadLaunchProvider

**Contract**: [`IProjectHotReloadLaunchProvider.cs`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/IProjectHotReloadLaunchProvider.cs)  
**VS Implementation**: [`LaunchProfilesDebugLaunchProvider`](../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Debug/LaunchProfilesDebugLaunchProvider.cs)  
**Scope**: Configured Project Level  
**Tests**: [`LaunchProfilesDebugLaunchProviderTests.cs`](../tests/Microsoft.VisualStudio.ProjectSystem.Managed.VS.UnitTests/ProjectSystem/VS/Debug/LaunchProfilesDebugLaunchProviderTests.cs)

The `LaunchProfilesDebugLaunchProvider` includes comprehensive Hot Reload integration tests:

**Hot Reload Integration Tests**:
- `LaunchWithProfileAsync_WhenHotReloadEnabled_CreatesHotReloadSession` - Verifies session creation for Project command profiles
- `LaunchWithProfileAsync_WhenHotReloadDisabled_DoesNotCreateHotReloadSession` - Verifies no session creation when disabled
- `LaunchWithProfileAsync_WhenNotProjectCommand_DoesNotCreateHotReloadSession` - Verifies no session creation for non-Project commands (e.g., IISExpress)

**Test Patterns**:
- Uses `IHotReloadOptionServiceFactory.Create()` for mocking global Hot Reload settings
- Verifies `TryCreatePendingSessionAsync()` is called with correct parameters including environment variables from `DebugLaunchSettings`
- Tests different launch profile command types and their Hot Reload eligibility

Interface responsible for launching projects with Hot Reload support.

**Key Responsibilities**:
- Launch projects with Hot Reload session integration
- Coordinate with session manager for launch-time session creation

## Hot Reload Session

### ProjectHotReloadSession
**Implementation**: [`ProjectHotReloadSession`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/HotReload/ProjectHotReloadSession.cs)  
**Interfaces**: `IProjectHotReloadSession`, `IProjectHotReloadSessionInternal`, `IManagedHotReloadAgent`  
**Tests**: [`ProjectHotReloadSessionTests.cs`](../tests/Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests/ProjectSystem/HotReload/ProjectHotReloadSessionTests.cs)  

The `IProjectHotReloadSession` represents an active Hot Reload session for a specific project and process. It is created by the `IProjectHotReloadAgent` factory and manages the lifecycle of the Hot Reload session, coordinating between the project system and the VS debugger infrastructure.

**Key Responsibilities**:
- Session lifecycle management (start, stop, apply changes)
- Communication with the Hot Reload agent manager
- Delta application to running processes
- Environment variable setup for Hot Reload support
- Process restart capabilities
- Diagnostic reporting and error handling

**Key Methods**:
- `StartSessionAsync()` - Initializes the session and registers with debugger
- `StopSessionAsync()` - Cleanly terminates the session
- `ApplyChangesAsync()` - Triggers application of pending changes
- `ApplyLaunchVariablesAsync()` - Sets up environment variables for the target process
- `RestartAsync()` - Rebuilds and relaunches the application

## Integration with LaunchTargetProvider and DebugLaunchProvider

### LaunchProfilesDebugLaunchProvider Integration

The [`LaunchProfilesDebugLaunchProvider`](../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Debug/LaunchProfilesDebugLaunchProvider.cs) serves as the primary integration point between the launch system and Hot Reload. It implements `IProjectHotReloadLaunchProvider` and coordinates the launch process with Hot Reload session creation.

**Tests**: [`LaunchProfilesDebugLaunchProviderTests.cs`](../tests/Microsoft.VisualStudio.ProjectSystem.Managed.VS.UnitTests/ProjectSystem/VS/Debug/LaunchProfilesDebugLaunchProviderTests.cs)

**Integration Points**:

1. **Session Creation During Launch**:
   ```csharp
   // In LaunchWithProfileAsync - after OnBeforeLaunchAsync but before debugger launch
   if (await HotReloadShouldBeEnabledAsync(profile, launchOptions)
       && targets.FirstOrDefault(x => x is DebugLaunchSettings) is DebugLaunchSettings consoleTargetSettings)
   {
       await _hotReloadSessionManager.Value.TryCreatePendingSessionAsync(
           configuredProject: _configuredProject,
           launchProvider: this,
           consoleTargetSettings.Environment,
           launchOptions,
           profile);
   }
   ```

2. **Hot Reload Eligibility Determination**:
   - `HotReloadShouldBeEnabledAsync()` performs comprehensive validation:
     - Profile command type validation using `IsRunProjectCommand()`
     - Profile-level Hot Reload setting using `IsHotReloadEnabled()`
     - Remote debugging check using `IsRemoteDebugEnabled()`
     - Profiling mode exclusion
     - Global Hot Reload option service validation

3. **Environment Variable Setup**:
   - Hot Reload requires specific environment variables to be set in the target process
   - Environment variables are extracted from `DebugLaunchSettings.Environment`
   - Only console target settings (of type `DebugLaunchSettings`) are used for Hot Reload

3. **Process Association**:
   - After successful launch, the session manager associates the Hot Reload session with the running process
   - This enables change application and process monitoring

### ProjectLaunchTargetsProvider Integration

The [`ProjectLaunchTargetsProvider`](../src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Debug/ProjectLaunchTargetsProvider.cs) handles the actual execution of project launch commands and integrates with Hot Reload through several mechanisms:

**Tests**: [`ProjectLaunchTargetsProviderTests.cs`](../tests/Microsoft.VisualStudio.ProjectSystem.Managed.VS.UnitTests/ProjectSystem/VS/Debug/ProjectLaunchTargetsProviderTests.cs)

**Integration Points**:

1. **Launch-Time Session Setup**:
   - Hot Reload session creation happens during `LaunchWithProfileAsync()` in `LaunchProfilesDebugLaunchProvider`
   - Session is created after `OnBeforeLaunchAsync()` but before debugger launch via `IVsDebuggerLaunchAsync`
   - Only specific target types (`DebugLaunchSettings`) trigger Hot Reload session creation

2. **Post-Launch Activation**:
   - `OnAfterLaunchAsync()` callback - Activates Hot Reload session after successful launch
   - Associates session with the running process via process ID

3. **Process Information**:
   - Provides process ID and other runtime information to the Hot Reload system
   - Enables session activation and monitoring

### Launch Flow with Hot Reload
1. **Launch-Time Session Creation**:
   - `LaunchProfilesDebugLaunchProvider.LaunchWithProfileAsync()` creates Hot Reload session after calling `QueryDebugTargetsInternalAsync()` and `OnBeforeLaunchAsync()`
   - Hot Reload eligibility is determined by `HotReloadShouldBeEnabledAsync()` which checks:
     - Profile uses "Project" command (`IsRunProjectCommand()`)
     - Hot Reload is enabled in profile settings (`IsHotReloadEnabled()`)
     - Remote debugging is not enabled (`IsRemoteDebugEnabled()`)
     - Not running under profiling mode
     - Global Hot Reload option is enabled via `IHotReloadOptionService`
   - Only creates session for `DebugLaunchSettings` console target settings
   - `ProjectHotReloadSessionManager.TryCreatePendingSessionAsync()` creates pending session
   - Environment variables are configured for Hot Reload support

2. **Post-Launch**:
   - `ProjectHotReloadSessionManager.ActivateSessionAsync()` activates the session
   - Session is associated with the running process via `OnAfterLaunchAsync()` callback
   - Process monitoring begins

3. **Runtime**:
   - Code changes trigger delta compilation
   - `ApplyChangesAsync()` applies deltas to running process
   - Session manages update application and error reporting

### Error Handling and Diagnostics

The Hot Reload system provides comprehensive error handling and diagnostic information:

**Diagnostic Channels**:
- `IHotReloadDiagnosticOutputService` - Output window logging
- Session-specific diagnostic messages with verbosity levels
- Process monitoring and exit detection

**Error Scenarios**:
- Build failures during updates
- Process termination during session
- Update application failures
- Capability validation failures

**Recovery Mechanisms**:
- Automatic session cleanup on process exit
- Graceful degradation when Hot Reload is not supported
- Restart capabilities for fatal errors

## Additional Test Coverage

Beyond the core MEF component tests, Hot Reload functionality is also covered by:

**Launch Profile Properties Tests**:
- [`ProjectLaunchProfileExtensionValueProviderTests.cs`](../tests/Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests/ProjectSystem/Properties/LaunchProfiles/ProjectLaunchProfileExtensionValueProviderTests.cs) - Tests for Hot Reload enabled property handling in launch profiles

**Hot Reload Specific Extension Methods Tests**:
- Extension methods in [`LaunchProfileExtensions.cs`](../src/Microsoft.VisualStudio.ProjectSystem.Managed/ProjectSystem/Debug/LaunchProfileExtensions.cs) provide:
  - `IsRunProjectCommand()` - Validates "Project" command type
  - `IsHotReloadEnabled()` - Checks profile-level Hot Reload setting (defaults to true)
  - `IsRemoteDebugEnabled()` - Validates remote debugging is not enabled
  - Profile setting accessors via `TryGetSetting()` method

**Test Mocking Infrastructure**:
- `IHotReloadOptionServiceFactory.Create()` - Creates mock Hot Reload option service with configurable debugging/non-debugging settings
- Tests validate proper parameter passing to `TryCreatePendingSessionAsync()` including environment variables from `DebugLaunchSettings`
- Comprehensive coverage of different launch profile command types and their Hot Reload eligibility
