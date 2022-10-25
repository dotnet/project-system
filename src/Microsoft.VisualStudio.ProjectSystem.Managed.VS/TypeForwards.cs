// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 //  Type Forwarding may be removed in the future, when we accept breaking changes.

[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Extensibility.IProjectExportProvider))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.DependenciesChangedEventArgs))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.DependencyTreeFlags))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IDependenciesChanges))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IDependencyModel))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.IProjectDependenciesSubTreeProvider))]
[assembly: TypeForwardedTo(destination: typeof(Microsoft.VisualStudio.ProjectSystem.VS.Tree.IFileIconProvider))]

#pragma warning restore RS0016
