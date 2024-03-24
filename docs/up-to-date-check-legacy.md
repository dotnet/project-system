﻿# Up-to-date Check (Legacy)

> [!IMPORTANT]
> This document relates to legacy C#/VB projects (.NET Framework-era) projects only!
>
> Other project types use different mechanisms for their up-to-date checks:

> - SDK-style C#/VB/F# projects (.NET-era) have a different check ([documentation](up-to-date-check.md)).
> - Visual C++ projects (`.vcxproj`) use `.tlog` files, which contain all build inputs and outputs ([documentation](https://learn.microsoft.com/visualstudio/extensibility/visual-cpp-project-extensibility#incremental-builds-and-up-to-date-checks)).

## Viewing logs

There is no built-in way to enable up-to-date check logging for old-style (non-SDK) projects. The [Tweakster extension](https://github.com/madskristensen/Tweakster#up-to-date-check-verbose) provides a UI option for this, however.

Alternatively, to enable logging manually:

1. Open a "Developer Command Prompt" for the particular version of Visual Studio you are using.
2. Enter command:
   ```text
   vsregedit set "%cd%" HKCU General U2DCheckVerbosity dword 1
   ```
3. The message `Set value for U2DCheckVerbosity` should be displayed

Run the same command with a `0` instead of a `1` to disable this logging.

Note that `"%cd%"` evaluates to the current directory. When you first open a Developer Prompt, this path will be correct. To execute this command from arbitrary locations, you'll need to substitute the relevant quoted path, such as `"C:\Program Files\Microsoft Visual Studio\2022\Enterprise"`, with no trailing `/` or `>` character.

You can change this value while VS is running and it will take effect immediately.

When logging is enabled you'll see messages such as this in build output:

> Project 'MyProject' is not up to date. Input file 'c:\path\myproject\class1.cs' is modified after output file 'C:\Path\MyProject\bin\Debug\MyProject.pdb'.

### Logging from the experimental hive

If you wish to enable this logging for a particular hive (this is an advanced scenario) then pass the hive's name after the path. For example:

```text
vsregedit set "%cd%" Exp HKCU General U2DCheckVerbosity dword 1
```
