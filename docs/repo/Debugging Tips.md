### Debugging Tips
#### Diagnosing Design-Time Builds

See [Diagnostic Design-Time Builds](/docs/design-time-builds.md#diagnosing-design-time-builds).

#### CPS Tracing

If you run the ProjectSystemDogfoodSetup project to launch Visual Studio, a trace listener is hooked up to output CPS tracing to the Debug category of the Output Window. You can use this to diagnose lots of issues, such as failing rules or missing snapshots.

You can increase the verbosity of what is output to the window by changing the verbosity level in `DogfoodProjectSystemPackage`.

#### Capabilities

You can see the active capabilities for a given project, by turning on the `DiagnoseCapabilities` capability:

``` XML
  <ItemGroup>
    <ProjectCapability Include="DiagnoseCapabilities"/>
  </ItemGroup>
  
```

This will add a node in Solution Expolorer that will list the current 'active' capabilities:

![image](https://cloud.githubusercontent.com/assets/1103906/22411354/16dccb2a-e6f7-11e6-91dc-91c451cc6371.png)
