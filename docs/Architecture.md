### Assembly Architecture

![image](https://cloud.githubusercontent.com/assets/1103906/14921086/248166fa-0de5-11e6-86fa-e96240b7fd07.png)

#### Laying Rules:

1. Assemblies cannot depend on the layer above it
2. Assemblies below the Visual Studio layer cannot depend on the Visual Studio SDK (no dependency on IVsXXX) nor any CPS-VS assembly. This layer and below should be hostable and usable outside of Visual Studio.
3. C# assemblies cannot depend on any VB assemblies and vice versa.

|Assembly|Description|
|:-------|:----------|
|__Visual Studio Layer__|
|__Microsoft.VisualStudio.ProjectSystem.CSharp.VS__| Contains components to enable the opening and editing of C# projects within Visual Studio.|
|__Microsoft.VisualStudio.ProjectSystem.VisualBasic.VS__| Contains components to enable the opening and editing of VB projects within Visual Studio.|
|__Microsoft.VisualStudio.ProjectSystem.Managed.VS__| Contains components that are shared between C# and VB projects. Make note that eventually the expectation is that F# if it moves to CPS will eventually share much of its code with C#/VB.|
|__Microsoft.VisualStudio.AppDesigner__| Contains the "Application Designer", which hosts property pages in a document window.|
|__Microsoft.VisualStudio.Editors__| Contains resources and settings editors, and C#/VB property pages.|
|__Host-Agnostic Layer__|
|__Microsoft.VisualStudio.ProjectSystem.CSharp__| Contains components to enable the opening and editing of C# projects agnostic of host.|
|__Microsoft.VisualStudio.ProjectSystem.VisualBasic__| Contains components to enable the opening and editing of VB projects agnostic of host.|
|__Microsoft.VisualStudio.ProjectSystem.Managed__| Contains components that are shared between C# and VB projects agnostic of host.|

Long term we expect more assemblies as we pivot and componentize based on:

AppModels
- WinForms
-	WPF
-	Web
-	CoreCLR
