# Debugging Design-Time Builds

## Diagnosing Design-Time Builds

See [Diagnosing Design-Time Builds](/docs/design-time-builds.md#diagnosing-design-time-builds).

## Failing Design-Time Builds

You can artificially fail a design-time build with the following:

``` XML
  <Target Name="FailDesignTimeBuild" AfterTargets="ResolveAssemblyReferences">
      <Error Text="Failed design-time build" />
  </Target>
```
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

## Measuring Design-Time Builds

An easy way of measuring performance of a design-time build outside of VS is to run something like (replacing solution paths with appropriate solution):

> msbuild /nologo /m:1 /v:m /clp:Summary;PerformanceSummary /t:CollectResolvedSDKReferencesDesignTime;DebugSymbolsProjectOutputGroup;ResolveComReferencesDesignTime;ContentFilesProjectOutputGroup;DocumentationProjectOutputGroupDependencies;SGenFilesOutputGroup;ResolveProjectReferencesDesignTime;SourceFilesProjectOutputGroup;DebugSymbolsProjectOutputGroupDependencies;SatelliteDllsProjectOutputGroup;BuiltProjectOutputGroup;SGenFilesOutputGroupDependencies;ResolveAssemblyReferencesDesignTime;CollectSDKReferencesDesignTime;DocumentationProjectOutputGroup;PriFilesOutputGroup;BuiltProjectOutputGroupDependencies;SatelliteDllsProjectOutputGroupDependencies /p:"SolutionFileName=**Roslyn.sln**;LangName=en-US;Configuration=Debug;LangID=1033;DesignTimeBuild=true;SolutionDir=**E:\\roslyn\\**;SolutionExt=.sln;BuildingInsideVisualStudio=true;DefineExplicitDefaults=true;Platform=AnyCPU;SolutionPath=**E:\\roslyn\\Roslyn.sln**;SolutionName=**Roslyn**;DevEnvDir=C:\Program Files (x86)\Microsoft Visual Studio\Enterprise\Common7\IDE;BuildingProject=false" **E:\roslyn\src\Workspaces\CSharp\Portable\CSharpWorkspace.csproj**

## Gathering full-fidelity binlogs

For many purposes the binlogs produced by Project System Tools are sufficient. Such logs do not contain all information, and so sometimes it's useful to use MSBuild's `MSBuildDebugEngine` environment variable to cause full-fidelity binlogs to be written to disk. When launching VS in the debugger, you can achieve this by modifying the `launchSettings.json` file directly with a change such as this:

```diff
diff --git a/setup/ProjectSystemSetup/Properties/launchSettings.json b/setup/ProjectSystemSetup/Properties/launchSettings.json
index d73e78560..e10d637c8 100644
--- a/setup/ProjectSystemSetup/Properties/launchSettings.json
+++ b/setup/ProjectSystemSetup/Properties/launchSettings.json
@@ -9,7 +9,9 @@
         "FSharpDesignTimeTargetsPath": "$(VisualStudioXamlRulesDir)Microsoft.FSharp.DesignTime.targets",
         "CSharpDesignTimeTargetsPath": "$(VisualStudioXamlRulesDir)Microsoft.CSharp.DesignTime.targets",
         "CPS_DiagnosticRuntime": "1",
-        "CPS_MetricsCollection": "1"
+        "CPS_MetricsCollection": "1",
+        "MSBuildDebugEngine": "1",
+        "MSBUILDDEBUGPATH": "c:/binlogs"
       }
     },
     "Start (with native debugging)": {
```

Make sure the `MSBUILDDEBUGPATH` exists on your machine.
