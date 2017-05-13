# Debugging Tips
## Diagnosing Design-Time Builds

See [Diagnostic Design-Time Builds](/docs/design-time-builds.md#diagnosing-design-time-builds).

## Delaying Design-Time Builds

You can artificially delay a design-time build with the following:

``` XML
  <UsingTask TaskName="Sleep" TaskFactory="CodeTaskFactory" AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.v4.0.dll">
  <ParameterGroup>
    <!-- Delay in milliseconds -->
    <Delay ParameterType="System.Int32" Required="true" />
  </ParameterGroup>
  <Task>
    <Code Type="Fragment" Language="cs">
      <![CDATA[
System.Threading.Thread.Sleep(this.Delay);
]]>
    </Code>
  </Task>
</UsingTask>

  <Target Name="DelayDesignTimeBuild" AfterTargets="ResolveAssemblyReferences">
      <Sleep Delay="10000" />
  </Target>
```

## CPS Tracing

When you build the solution either in Visual Studio or via the command-line, a trace listener is hooked up to output CPS tracing to the Debug category of the Output Window under the `RoslynDev` Visual Studio instance. You can use this to diagnose lots of issues, such as failing rules or missing snapshots.

You can increase the verbosity of what is output to the window by changing the verbosity level in [ManagedProjectSystemPackage.DebuggingTraceListener](https://github.com/dotnet/roslyn-project-system/blob/master/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/Packaging/ManagedProjectSystemPackage.DebuggerTraceListener.cs#L44).

## Capabilities

You can see the active capabilities for a given project, by turning on the `DiagnoseCapabilities` capability:

``` XML
  <ItemGroup>
    <ProjectCapability Include="DiagnoseCapabilities"/>
  </ItemGroup>
```

This will add a node in Solution Explorer that will list the current 'active' capabilities:

![image](https://cloud.githubusercontent.com/assets/1103906/22411354/16dccb2a-e6f7-11e6-91dc-91c451cc6371.png)

## Crash Dumps

To get Windows to automatically save a memory dump every time Visual Studio crashes, merge the following registry settings:

[AlwaysSaveDevEnvCrashDumps.reg](/docs/repo/content/AlwaysSaveDevEnvCrashDumps.reg?raw=true)

Dumps will be saved to C:\Crashdumps.
