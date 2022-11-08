This document describes the interaction between CPS, the .NET Project System, the Debugger, and the rest of VS (hereafter just referred to as "VS") in three scenarios:

- Identifying startup projects
- Checking if a particular project can launch
- Launch (both with and without a debugger)

The emphasis here is on the interfaces used to communicate between those components; the internal operation of the components is not described. Also, the interaction between the .NET Project System and individual launch profile handlers (such as those implemented by the Web Tools team) is not described. Those handlers may have additional dependencies on other VS or Debugger interfaces.

## Identifying Startup Projects

VS wishes to know which projects can be startup projects. This flow is driven by project changes: CPS lets the .NET PS know every time the project changes, and the .NET PS checks if the active launch profile for the project can be run. The `IVsStartupProjectsListService` is then called to update it on whether or not the project is "startable".

This needs to happen on every project change because:
1. The active launch profile may have changed
2. The active launch profile handler may depend on arbitrary project state

### Relevant Interfaces

- IVsStartupProjectsListService (defined by VS, implemented by VS)

```mermaid
sequenceDiagram
  participant CPS
  participant .NET as .NET PS
  participant VS
  
  CPS->>.NET: <Project changed>
  activate .NET
  alt active profile is startable
    .NET->>VS: IVsStartupProjectsListService.AddProject
  else active profile is not startable
    .NET->>VS: IVsStartupProjectsListService.RemoveProject
  end
  deactivate .NET
```

## Checking If a Project Can Launch

VS calls into CPS to ask if a project can launch, and CPS delegates this request to the .NET Project System.

### Relevant Interfaces

- IVsDebuggableProjectCfg (defined by VS, implemented by CPS)
- IDebugLaunchProvider (defined by CPS, implemented by the .NET Project System)

```mermaid
sequenceDiagram
  participant VS
  participant CPS
  participant .NET as .NET PS

  
  VS->>CPS: IVsDebuggableProjectCfg.QueryDebugLaunch
  CPS->>.NET: IDebugLaunchProvider.CanLaunchAsync
  .NET-->>CPS: true
  CPS-->>VS: <result>
```

## Launch (both with and without debugging)

CPS provides some standard functionality for interacting with the debugger that the .NET Project System largely does not use (at least not for launching a plain .exe).

VS tells CPS to launch a project, and CPS delegates to the .NET Project System. The .NET Project System then fills in a standard CPS type (`DebugLaunchSettings`). It _could_ simply hand that back and let CPS handle calling the debugger, but it doesn't. Instead it calls a debugger API (`IVsDebuggerLaunchAsync`) directly. The debugger notifies us when the launch has occurred via a callback  (`IVsDebuggerLaunchCompletionCallback`).

Filling in the `DebugLaunchSettings` is accomplished by delegating to the `IDebugProfileLaunchTargetsProvider` that know how to handle the "commandName" specific in the profile. The .NET Project System provides a default implementation of this interface that handles the "Project" commandName. Other teams can provide their own implementations for other commandNames; e.g. the Web Tools team provides an implementation that handles "IISExpress".

### Relevant Interfaces

- IVsDebuggableProjectCfg (defined by VS, implemented by CPS)
- IDebugLaunchProvider (defined by CPS, implemented by the .NET Project System)
- IVsDebuggerLaunchAsync (defined and implemented by the debugger)
- IVsDebuggerLaunchCompletionCallback (defined by the debugger, implemented by the .NET Project System)
- IDebugProfileLaunchTargetsProvider (defined by the .NET Project System, implemented by the .NET Project System and various other teams).

```mermaid
sequenceDiagram

  participant VS
  participant CPS
  participant .NET as .NET PS
  participant PH as Profile Handler
  participant Debugger
  
  VS->>CPS: IVsDebuggableProjectCfg.DebugLaunch
  CPS->>.NET: IDebugLaunchProvider.LaunchAsync
  activate .NET
  .NET->>PH: IDebugProfileLaunchTargetsProvider.QueryDebugTargetsAsync
  activate PH
  PH->>CPS: Fill in instance of DebugLaunchSettings
  CPS-->>PH: <return>
  PH-->>.NET: <return>
  deactivate PH
  .NET->>PH: IDebugProfileLaunchTargetsProvider.OnBeforeLaunchAsync
  activate PH
  PH-->>.NET: <return>
  deactivate PH
  .NET->>Debugger: IVsDebuggerLaunchAsync.LaunchDebugTargetsAsync
  activate Debugger
  Note over Debugger: Process starts
  Debugger->>.NET: IVsDebuggerLaunchCompletionCallback.OnComplete
  deactivate Debugger
  .NET->>PH: IDebugProfileLaunchTargetsProvider.OnAfterLaunchAsync
  activate PH
  PH-->.NET: <return>
  deactivate PH
  deactivate .NET
```