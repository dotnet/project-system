### Debugging Tips

#### CPS Tracing

If you run the ProjectSystemDogfoodSetup project to launch Visual Studio, a trace listener is hooked up to output CPS tracing to the Debug category of the Output Window. You can use this to diagnose lots of issues, such as failing rules or missing snapshots.

You can increase the verbosity of what is output to the window by changing the verbosity level in `DogfoodProjectSystemPackage`.

#### Diagnosing Design-Time Builds

See [Diagnostic Design-Time Builds](/docs/design-time-builds.md#diagnosing-design-time-builds).
