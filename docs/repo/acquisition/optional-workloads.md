Add Support for Optional .NET Workloads

# Background
- https://github.com/dotnet/designs/blob/main/accepted/2020/workloads/workloads.md
- https://github.com/dotnet/designs/blob/main/accepted/2020/workloads/workload-resolvers.md

# Description
Optional workloads have been introduced into .NET to enable the SDK to scale without shipping huge monolithic SDKs as .NET support of additional features and platforms (e.g. iOS and Android) grows. As a result, Visual Studio needs to detect when projects are loaded that require optional workloads that are not installed. The in-product acquisition experience should then present users with the list of all components that need to be installed in order to successfully load and use their projects.

# Design
The .NET SDK contains targets that detect when workloads required by the project are absent and then fail the build if there were missing workloads. The missing workloads are output as [SuggestedWorkload] MSBuild items. See https://github.com/dotnet/sdk/blob/release/6.0.1xx-preview7/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.ImportWorkloads.targets for more details. Note that MSBuild project evaluation needs to happen to gather the list of missing workloads.

CPS provides support for extensions vetoing project loads via the IVetoProjectLoad.AllowProjectLoadAsync interface. This PR implements this interface and uses the results of MSBuild project evaluation to determine whether there are missing workloads. Should any be found, then this IVetoProjectLoad implementation vetos the loading, which causes Visual Studio to cancel the project load leaving it unloaded in the solution.

The Visual Studio setup services query the unloaded projects in the solution to detect which components are missing and consequently display a banner letting the user know that they need to install extra components. The setup system also needs to present list of missing components to the user when they click on the "Install" button. The evaluation results from MSBuild contain the list of component IDs (for the missing workloads) required by the setup system. Therefore, the project load veto implementation also needs to register this set of component IDs with the setup system. This is done via a new IMissingWorkloadRegistrationService, which is obtained using the service broker mechanism.

The IMissingWorkloadRegistrationService interface is defined in the RPC contracts in PR https://devdiv.visualstudio.com/DevDiv/_git/VS.RPC.Contracts/pullrequest/340386
IMissingWorkloadRegistrationService is implemented in the Visual Studio shell's setup Package in PR https://devdiv.visualstudio.com/DevDiv/_git/VS/pullrequest/340388
