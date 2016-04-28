## Welcome to the "Roslyn" C# and Visual Basic project system

In the Visual Studio "15" timeframe, the C# and Visual Basic project systems will be rewritten on top of the new [Visual Studio Common Project System ("CPS")](https://blogs.msdn.microsoft.com/visualstudio/2015/06/02/introducing-the-project-system-extensibility-preview/).

The current C# and Visual Basic project systems ("csproj.dll" and "msvbprj.dll"), which first shipped back in Visual Studio .NET nearly 15 years ago, are:

- Native and COM-based
- Single threaded and bound to the UI thread
- Hard to extend outside of aggregation via the use of [sub types ("flavors")](https://msdn.microsoft.com/en-us/library/bb166488.aspx)
- Tied to Visual Studio

The new C# and Visual Basic project system, built on top of CPS, will be:

- Managed and managed-interface based
- Multi-threaded, scalable, and responsive
- Easy to extend via the use of the  Managed Extensibility Framework ("MEF") and composable. Many parties, including 3rd parties, can contribute to a single project system
- Hostable outside of Visual Studio

## What is a project system?
A project system sits between and coordinates with a project file on disk (for example, .csproj and .vbproj) and various Visual Studio features including, but not limited to, Solution Explorer, designers, the debugger, language services, build and deployment. Features that interact with files contained in a project file, almost always do it through the project system.
