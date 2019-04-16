using System.Runtime.CompilerServices;

#pragma warning disable RS0016 //  Type Forwarding will be removed.

[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Extensibility.IProjectExportProvider))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.ManagedImageMonikers))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.DependenciesChangedEventArgs))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.DependencyTreeFlags))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IDependenciesChanges))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IDependencyModel))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IProjectDependenciesSubTreeProvider))]

#pragma warning restore RS0016 // Do not add multiple public overloads with optional parameters
