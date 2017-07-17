# Debugging Tips

## Design-Time Builds
### Diagnosing Design-Time Builds

See [Diagnostic Design-Time Builds](/docs/design-time-builds.md#diagnosing-design-time-builds).

### Failing Design-Time Builds

You can artificially fail a design-time build with the following:

``` XML
  <Target Name="FailDesignTimeBuild" AfterTargets="ResolveAssemblyReferences">
      <Error Text="Failed design-time build" />
  </Target>
```
### Delaying Design-Time Builds

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

## UI Delays

To get Windows to automatically send on data about UI delays in Visual Studio, merge the following registry settings:

[AlwaysSendPerfWatsonData.reg](/docs/repo/content/AlwaysSendPerfWatsonData.reg?raw=true)

For more information on these settings, see [Configure Windows telemetry in your organization](https://docs.microsoft.com/en-us/windows/configuration/configure-windows-telemetry-in-your-organization).

## Testing SDK Changes

If you're making changes to the SDK (that is, the [dotnet/sdk](https://github.com/dotnet/sdk) repo) you can easily test VS or msbuild.exe with those changes by setting the `DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR` environment variable.

After you build, find the generated Sdks directory. For example, if your repo is at D:\Projects\sdk, you'll find it at D:\Projects\sdk\bin\Debug\Sdks. Set the environment variable to point to this location:

`set DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR=D:\Projects\sdk\bin\Debug\Sdks`

Now any instances of msbuild.exe or VS that inherit that setting will use your locally-produced SDK.

## Figuring out Git SHA for a given VS build

1. Visit: http://vsbuildtoroslynsha.azurewebsites.net/, choose Project System from the down down, and enter the branch and build number from Help -> About:

![image](https://user-images.githubusercontent.com/1103906/28194295-a2ad6f36-6886-11e7-95d4-78b7dd191744.png)
