# Crash Dumps, UI Delays and Hang Data

## Crash Dumps

To get Windows to automatically save a memory dump every time Visual Studio crashes, merge the following registry settings:

[AlwaysSaveDevEnvCrashDumps.reg](/docs/repo/content/AlwaysSaveDevEnvCrashDumps.reg?raw=true)

Dumps will be saved to C:\Crashdumps.

## Non-Fatal Watsons

To view Visual Studio's non-fatal watson reports on a machine:

1. Open up Event Viewer
2. Right-click on Custom Views and choose Import Custom View
3. In file name, point to [NonFatalWatsons.xml](/docs/repo/content/NonFatalWatsons.xml?raw=true) and click OK

## UI Delays

To get Windows to automatically send on data about UI delays in Visual Studio, merge the following registry settings:

[AlwaysSendPerfWatsonData.reg](/docs/repo/content/AlwaysSendPerfWatsonData.reg?raw=true)

For more information on these settings, see [Configure Windows telemetry in your organization](https://docs.microsoft.com/en-us/windows/configuration/configure-windows-telemetry-in-your-organization).
