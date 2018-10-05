# Logging

The project system code logs information to a custom Output Window pane either
while debugging or when a certain environment variable is set.

## Inspecting Log While Debugging

When you build this repository under debug either within Visual Studio or via
the command-line, an extra output window pane will be created that contains a
log of project-system related events.

## Collecting Log for a Release Build

Run VS with the `PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED` set to `1`.  For
example:

1. Start a Developer Command Prompt
2. Run: `set PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED=1`
3. Run: `devenv`
4. Open a solution
5. Use "View.Output Window"
6. Select the pane titled "Project" from the dropdown
